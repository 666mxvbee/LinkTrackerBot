using System.Globalization;
using System.Text.Json;
using LinkTracker.Scrapper.Configuration;
using LinkTracker.Shared.Models;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace LinkTracker.Scrapper.Services.Notifications;

public sealed class OutboxMessageSender(
    NpgsqlDataSource dataSource,
    IOptions<NotificationOptions> options) : IMessageSender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SendAsync(LinkUpdate update, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(update, JsonOptions);

        await using var command = dataSource.CreateCommand("""
            INSERT INTO notification_outbox (message_id, topic, message_key, payload)
            VALUES (@messageId, @topic, @messageKey, @payload);
            """);

        command.Parameters.AddWithValue("messageId", Guid.NewGuid());
        command.Parameters.AddWithValue("topic", options.Value.Kafka.Topic);
        command.Parameters.AddWithValue("messageKey", BuildMessageKey(update));
        command.Parameters.Add("payload", NpgsqlDbType.Jsonb).Value = payload;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string BuildMessageKey(LinkUpdate update)
    {
        return update.Id > 0
            ? update.Id.ToString(CultureInfo.InvariantCulture)
            : update.Url;
    }
}
