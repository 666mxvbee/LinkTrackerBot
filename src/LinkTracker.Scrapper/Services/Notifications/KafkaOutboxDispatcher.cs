using LinkTracker.Scrapper.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LinkTracker.Scrapper.Services.Notifications;

public sealed class KafkaOutboxDispatcher(
    NpgsqlDataSource dataSource,
    IKafkaMessagePublisher publisher,
    IOptions<NotificationOptions> options,
    ILogger<KafkaOutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.Kafka.OutboxDispatchIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch Kafka outbox batch");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    internal async Task DispatchPendingBatchAsync(CancellationToken cancellationToken)
    {
        var batchSize = Math.Clamp(options.Value.Kafka.OutboxBatchSize, 1, 500);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var messages = await LoadPendingMessagesAsync(connection, transaction, batchSize, cancellationToken);

        foreach (var message in messages)
        {
            await DispatchMessageAsync(connection, transaction, message, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task DispatchMessageAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await publisher.PublishAsync(message.Topic, message.Key, message.Payload, cancellationToken);
            await MarkProcessedAsync(connection, transaction, message.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to publish outbox message {OutboxMessageId}. It will remain pending",
                message.Id);

            await MarkFailedAttemptAsync(connection, transaction, message.Id, ex.Message, cancellationToken);
        }
    }

    private static async Task<OutboxMessage[]> LoadPendingMessagesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int batchSize,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT id, topic, message_key, payload::text
            FROM notification_outbox
            WHERE status = 'Pending'
            ORDER BY created_at, id
            LIMIT @batchSize
            FOR UPDATE SKIP LOCKED;
            """, connection, transaction);

        command.Parameters.AddWithValue("batchSize", batchSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var messages = new List<OutboxMessage>();

        while (await reader.ReadAsync(cancellationToken))
        {
            messages.Add(new OutboxMessage(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3)));
        }

        return messages.ToArray();
    }

    private static async Task MarkProcessedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long id,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            UPDATE notification_outbox
            SET status = 'Processed',
                attempt_count = attempt_count + 1,
                last_error = NULL,
                processed_at = NOW()
            WHERE id = @id;
            """, connection, transaction);

        command.Parameters.AddWithValue("id", id);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkFailedAttemptAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long id,
        string error,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            UPDATE notification_outbox
            SET attempt_count = attempt_count + 1,
                last_error = @lastError
            WHERE id = @id;
            """, connection, transaction);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("lastError", error);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed record OutboxMessage(long Id, string Topic, string? Key, string Payload);
}
