using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using LinkTracker.Bot.Configuration;
using LinkTracker.Bot.Services.Notifications;
using LinkTracker.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Tests.Kafka;

[Collection(KafkaCollection.Name)]
public class KafkaLinkUpdateConsumerTests(KafkaFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Consumer_HandlesValidLinkUpdate()
    {
        var topics = await CreateTopicsAsync();
        var handler = new CapturingLinkUpdateHandler();
        using var consumer = CreateConsumer(topics, handler);

        await consumer.StartAsync(CancellationToken.None);

        try
        {
            var update = new LinkUpdate(
                1,
                "https://github.com/octo/repo",
                "new issue",
                [1001]);

            await KafkaTestSupport.ProduceAsync(
                fixture.BootstrapServers,
                topics.Topic,
                "link-1",
                JsonSerializer.Serialize(update, JsonOptions));

            await WaitUntilAsync(() => handler.Handled.Count == 1);

            Assert.Equal(update.Url, handler.Handled.Single().Url);
            Assert.Equal(update.Description, handler.Handled.Single().Description);
        }
        finally
        {
            await consumer.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Consumer_SendsInvalidJsonToDlq()
    {
        var topics = await CreateTopicsAsync();
        var handler = new CapturingLinkUpdateHandler();
        using var consumer = CreateConsumer(topics, handler);

        await consumer.StartAsync(CancellationToken.None);

        try
        {
            await KafkaTestSupport.ProduceAsync(
                fixture.BootstrapServers,
                topics.Topic,
                "bad-json",
                "{not valid json");

            var dlqMessage = KafkaTestSupport.ConsumeSingle(
                fixture.BootstrapServers,
                topics.DlqTopic,
                TimeSpan.FromSeconds(15));

            Assert.Contains("DeserializationError", dlqMessage.Message.Value);
            Assert.Empty(handler.Handled);
        }
        finally
        {
            await consumer.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Consumer_SendsInvalidDataToDlq()
    {
        var topics = await CreateTopicsAsync();
        var handler = new CapturingLinkUpdateHandler();
        using var consumer = CreateConsumer(topics, handler);

        await consumer.StartAsync(CancellationToken.None);

        try
        {
            var invalidUpdate = new LinkUpdate(1, "", "new issue", [1001]);

            await KafkaTestSupport.ProduceAsync(
                fixture.BootstrapServers,
                topics.Topic,
                "invalid-data",
                JsonSerializer.Serialize(invalidUpdate, JsonOptions));

            var dlqMessage = KafkaTestSupport.ConsumeSingle(
                fixture.BootstrapServers,
                topics.DlqTopic,
                TimeSpan.FromSeconds(15));

            Assert.Contains("ValidationError", dlqMessage.Message.Value);
            Assert.Empty(handler.Handled);
        }
        finally
        {
            await consumer.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Consumer_RetriesProcessingErrorAndSendsMessageToDlq()
    {
        var topics = await CreateTopicsAsync();
        var handler = new CapturingLinkUpdateHandler(shouldFail: true);
        using var consumer = CreateConsumer(topics, handler, maxRetryAttempts: 2);

        await consumer.StartAsync(CancellationToken.None);

        try
        {
            var update = new LinkUpdate(
                1,
                "https://github.com/octo/repo",
                "new issue",
                [1001]);

            await KafkaTestSupport.ProduceAsync(
                fixture.BootstrapServers,
                topics.Topic,
                "processing-error",
                JsonSerializer.Serialize(update, JsonOptions));

            var dlqMessage = KafkaTestSupport.ConsumeSingle(
                fixture.BootstrapServers,
                topics.DlqTopic,
                TimeSpan.FromSeconds(15));

            Assert.Equal(2, handler.Attempts);
            Assert.Contains("ProcessingError", dlqMessage.Message.Value);
        }
        finally
        {
            await consumer.StopAsync(CancellationToken.None);
        }
    }

    private async Task<KafkaTopics> CreateTopicsAsync()
    {
        var topics = new KafkaTopics(
            $"link-updates-{Guid.NewGuid():N}",
            $"link-updates-dlq-{Guid.NewGuid():N}");

        await KafkaTestSupport.CreateTopicAsync(fixture.BootstrapServers, topics.Topic);
        await KafkaTestSupport.CreateTopicAsync(fixture.BootstrapServers, topics.DlqTopic);

        return topics;
    }

    private KafkaLinkUpdateConsumer CreateConsumer(
        KafkaTopics topics,
        ILinkUpdateHandler handler,
        int maxRetryAttempts = 3)
    {
        var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = fixture.BootstrapServers,
            Acks = Acks.All
        }).Build();

        return new KafkaLinkUpdateConsumer(
            Options.Create(new NotificationOptions
            {
                Transport = NotificationTransports.Kafka,
                Kafka = new KafkaConsumerOptions
                {
                    BootstrapServers = fixture.BootstrapServers,
                    Topic = topics.Topic,
                    DlqTopic = topics.DlqTopic,
                    GroupId = $"linktracker-bot-test-{Guid.NewGuid():N}",
                    MaxRetryAttempts = maxRetryAttempts,
                    RetryDelayMilliseconds = 10
                }
            }),
            handler,
            new InMemoryNotificationDeduplicationStore(),
            producer,
            NullLogger<KafkaLinkUpdateConsumer>.Instance);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Expected Kafka consumer condition was not reached.");
    }

    private sealed class CapturingLinkUpdateHandler(bool shouldFail = false) : ILinkUpdateHandler
    {
        public ConcurrentBag<LinkUpdate> Handled { get; } = [];

        public int Attempts { get; private set; }

        public Task HandleAsync(LinkUpdate update, CancellationToken cancellationToken)
        {
            Attempts++;

            if (shouldFail)
            {
                throw new InvalidOperationException("Telegram unavailable");
            }

            Handled.Add(update);
            return Task.CompletedTask;
        }
    }

    private sealed record KafkaTopics(string Topic, string DlqTopic);
}
