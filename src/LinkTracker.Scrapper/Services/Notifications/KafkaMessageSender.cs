using System.Globalization;
using System.Text.Json;
using LinkTracker.Scrapper.Configuration;
using LinkTracker.Shared.Models;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Services.Notifications;

public sealed class KafkaMessageSender(
    IKafkaMessagePublisher publisher,
    IOptions<NotificationOptions> options) : IMessageSender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SendAsync(LinkUpdate update, CancellationToken cancellationToken)
    {
        var topic = options.Value.Kafka.Topic;
        var payload = JsonSerializer.Serialize(update, JsonOptions);

        await publisher.PublishAsync(
            topic,
            BuildMessageKey(update),
            payload,
            cancellationToken);
    }

    private static string BuildMessageKey(LinkUpdate update)
    {
        return update.Id > 0
            ? update.Id.ToString(CultureInfo.InvariantCulture)
            : update.Url;
    }
}
