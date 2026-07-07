using System.Globalization;
using System.Text.Json;
using Confluent.Kafka;
using LinkTracker.Scrapper.Configuration;
using LinkTracker.Shared.Models;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Services.Notifications;

public sealed class KafkaMessageSender(
    IProducer<string, string> producer,
    IOptions<NotificationOptions> options,
    ILogger<KafkaMessageSender> logger) : IMessageSender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SendAsync(LinkUpdate update, CancellationToken cancellationToken)
    {
        var topic = options.Value.Kafka.Topic;
        var payload = JsonSerializer.Serialize(update, JsonOptions);

        var result = await producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = BuildMessageKey(update),
                Value = payload,
            },
            cancellationToken);

        logger.LogInformation(
            "Published link update to Kafka topic {Topic} at {TopicPartitionOffset}",
            topic,
            result.TopicPartitionOffset);
    }

    private static string BuildMessageKey(LinkUpdate update)
    {
        return update.Id > 0
            ? update.Id.ToString(CultureInfo.InvariantCulture)
            : update.Url;
    }
}
