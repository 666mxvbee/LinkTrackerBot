using LinkTracker.Scrapper.Configuration;
using LinkTracker.Scrapper.Services.Notifications;
using LinkTracker.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LinkTracker.Scrapper.Tests.Postgres;

[Collection(PostgresCollection.Name)]
public class OutboxTests(PostgresFixture fixture)
{
    [Fact]
    public async Task OutboxMessageSender_WritesPendingMessage()
    {
        await fixture.ResetDatabaseAsync();

        var sender = CreateOutboxSender();

        await sender.SendAsync(
            new LinkUpdate(42, "https://github.com/octo/repo", "new issue", [1001]),
            CancellationToken.None);

        var row = await ReadSingleOutboxRowAsync();

        Assert.Equal("link-updates-test", row.Topic);
        Assert.Equal("42", row.MessageKey);
        Assert.Equal("Pending", row.Status);
        Assert.Contains("github.com/octo/repo", row.Payload);
    }

    [Fact]
    public async Task KafkaOutboxDispatcher_MarksMessageProcessedAfterSuccessfulPublish()
    {
        await fixture.ResetDatabaseAsync();

        var sender = CreateOutboxSender();
        var publisher = new CapturingKafkaPublisher();
        var dispatcher = CreateDispatcher(publisher);

        await sender.SendAsync(
            new LinkUpdate(0, "https://github.com/octo/repo", "new issue", [1001]),
            CancellationToken.None);

        await dispatcher.DispatchPendingBatchAsync(CancellationToken.None);

        var row = await ReadSingleOutboxRowAsync();

        Assert.Equal("Processed", row.Status);
        Assert.Equal(1, row.AttemptCount);
        Assert.Null(row.LastError);
        Assert.Single(publisher.Messages);
        Assert.Equal("https://github.com/octo/repo", publisher.Messages.Single().Key);
    }

    [Fact]
    public async Task KafkaOutboxDispatcher_KeepsMessagePendingWhenPublishFails()
    {
        await fixture.ResetDatabaseAsync();

        var sender = CreateOutboxSender();
        var publisher = new CapturingKafkaPublisher(shouldFail: true);
        var dispatcher = CreateDispatcher(publisher);

        await sender.SendAsync(
            new LinkUpdate(0, "https://github.com/octo/repo", "new issue", [1001]),
            CancellationToken.None);

        await dispatcher.DispatchPendingBatchAsync(CancellationToken.None);

        var row = await ReadSingleOutboxRowAsync();

        Assert.Equal("Pending", row.Status);
        Assert.Equal(1, row.AttemptCount);
        Assert.Contains("Kafka unavailable", row.LastError);
        Assert.Empty(publisher.Messages);
    }

    private OutboxMessageSender CreateOutboxSender()
    {
        return new OutboxMessageSender(
            fixture.DataSource,
            Options.Create(CreateOptions()));
    }

    private KafkaOutboxDispatcher CreateDispatcher(IKafkaMessagePublisher publisher)
    {
        return new KafkaOutboxDispatcher(
            fixture.DataSource,
            publisher,
            Options.Create(CreateOptions()),
            NullLogger<KafkaOutboxDispatcher>.Instance);
    }

    private static NotificationOptions CreateOptions()
    {
        return new NotificationOptions
        {
            Transport = NotificationTransports.Kafka,
            Kafka = new KafkaProducerOptions
            {
                Topic = "link-updates-test",
                OutboxBatchSize = 10,
                OutboxDispatchIntervalSeconds = 1
            }
        };
    }

    private async Task<OutboxRow> ReadSingleOutboxRowAsync()
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT topic, message_key, payload::text, status, attempt_count, last_error
            FROM notification_outbox;
            """, connection);

        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());

        var row = new OutboxRow(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.IsDBNull(5) ? null : reader.GetString(5));

        Assert.False(await reader.ReadAsync());

        return row;
    }

    private sealed class CapturingKafkaPublisher(bool shouldFail = false) : IKafkaMessagePublisher
    {
        public List<(string Topic, string? Key, string Payload)> Messages { get; } = [];

        public Task PublishAsync(
            string topic,
            string? key,
            string payload,
            CancellationToken cancellationToken)
        {
            if (shouldFail)
            {
                throw new InvalidOperationException("Kafka unavailable");
            }

            Messages.Add((topic, key, payload));
            return Task.CompletedTask;
        }
    }

    private sealed record OutboxRow(
        string Topic,
        string MessageKey,
        string Payload,
        string Status,
        int AttemptCount,
        string? LastError);
}
