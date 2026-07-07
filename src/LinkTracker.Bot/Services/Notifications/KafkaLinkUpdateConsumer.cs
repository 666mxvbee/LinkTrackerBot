using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using LinkTracker.Bot.Configuration;
using LinkTracker.Shared.Models;
using Microsoft.Extensions.Options;

namespace LinkTracker.Bot.Services.Notifications;

public sealed class KafkaLinkUpdateConsumer(
    IOptions<NotificationOptions> options,
    ILinkUpdateHandler linkUpdateHandler,
    INotificationDeduplicationStore deduplicationStore,
    IProducer<string, string> dlqProducer,
    ILogger<KafkaLinkUpdateConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken cancellationToken)
    {
        var kafkaOptions = options.Value.Kafka;

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = kafkaOptions.BootstrapServers,
            GroupId = kafkaOptions.GroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            AllowAutoCreateTopics = false,
        }).Build();

        consumer.Subscribe(kafkaOptions.Topic);
        logger.LogInformation("Kafka link update consumer subscribed to topic {Topic}", kafkaOptions.Topic);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(cancellationToken);

                    ProcessMessageAsync(consumeResult, cancellationToken)
                        .GetAwaiter()
                        .GetResult();

                    consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Kafka link update consumer is stopping");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(
        ConsumeResult<string, string> consumeResult,
        CancellationToken cancellationToken)
    {
        if (!TryDeserialize(consumeResult, out var update, out var deserializationError))
        {
            await SendToDlqAsync(
                consumeResult,
                "DeserializationError",
                deserializationError,
                cancellationToken);
            return;
        }

        if (!TryValidate(update, out var validationError))
        {
            await SendToDlqAsync(
                consumeResult,
                "ValidationError",
                validationError,
                cancellationToken);
            return;
        }

        var fingerprint = BuildFingerprint(update);

        if (deduplicationStore.IsProcessed(fingerprint))
        {
            logger.LogInformation("Skipping already processed link update {Fingerprint}", fingerprint);
            return;
        }

        await HandleWithRetriesAsync(update, consumeResult, cancellationToken);
        deduplicationStore.MarkProcessed(fingerprint);
    }

    private static bool TryDeserialize(
        ConsumeResult<string, string> consumeResult,
        out LinkUpdate update,
        out string error)
    {
        try
        {
            var result = JsonSerializer.Deserialize<LinkUpdate>(consumeResult.Message.Value, JsonOptions);

            if (result is null)
            {
                update = default!;
                error = "Message payload is empty.";
                return false;
            }

            update = result;
            error = string.Empty;
            return true;
        }
        catch (JsonException ex)
        {
            update = default!;
            error = ex.Message;
            return false;
        }
    }

    private static bool TryValidate(LinkUpdate update, out string error)
    {
        if (string.IsNullOrWhiteSpace(update.Url))
        {
            error = "Url is required.";
            return false;
        }

        if (!Uri.TryCreate(update.Url, UriKind.Absolute, out _))
        {
            error = "Url must be absolute.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(update.Description))
        {
            error = "Description is required.";
            return false;
        }

        if (update.TgChatIds is null || update.TgChatIds.Length == 0)
        {
            error = "At least one Telegram chat id is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private async Task HandleWithRetriesAsync(
        LinkUpdate update,
        ConsumeResult<string, string> consumeResult,
        CancellationToken cancellationToken)
    {
        var kafkaOptions = options.Value.Kafka;
        var maxAttempts = Math.Max(1, kafkaOptions.MaxRetryAttempts);
        var retryDelay = TimeSpan.FromMilliseconds(Math.Max(0, kafkaOptions.RetryDelayMilliseconds));

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await linkUpdateHandler.HandleAsync(update, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Failed to process Kafka link update {Url}. Attempt {Attempt}/{MaxAttempts}",
                    update.Url,
                    attempt,
                    maxAttempts);

                if (retryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(retryDelay, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process Kafka link update {Url}. Sending message to DLQ",
                    update.Url);

                await SendToDlqAsync(
                    consumeResult,
                    "ProcessingError",
                    ex.Message,
                    cancellationToken);
                return;
            }
        }
    }

    private async Task SendToDlqAsync(
        ConsumeResult<string, string> consumeResult,
        string reason,
        string error,
        CancellationToken cancellationToken)
    {
        var kafkaOptions = options.Value.Kafka;

        var message = new KafkaDlqMessage(
            Reason: reason,
            Error: error,
            Payload: consumeResult.Message.Value,
            SourceTopic: consumeResult.Topic,
            SourcePartition: consumeResult.Partition.Value,
            SourceOffset: consumeResult.Offset.Value,
            FailedAt: DateTimeOffset.UtcNow);

        await dlqProducer.ProduceAsync(
            kafkaOptions.DlqTopic,
            new Message<string, string>
            {
                Key = consumeResult.Message.Key,
                Value = JsonSerializer.Serialize(message, JsonOptions),
            },
            cancellationToken);

        logger.LogWarning(
            "Sent Kafka message from {TopicPartitionOffset} to DLQ topic {DlqTopic}. Reason: {Reason}",
            consumeResult.TopicPartitionOffset,
            kafkaOptions.DlqTopic,
            reason);
    }

    private static string BuildFingerprint(LinkUpdate update)
    {
        var payload = $"{update.Id}|{update.Url}|{update.Description}|{string.Join(',', update.TgChatIds)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));

        return Convert.ToHexString(hash);
    }
}
