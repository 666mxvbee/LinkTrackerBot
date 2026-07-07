using Confluent.Kafka;

namespace LinkTracker.Scrapper.Services.Notifications;

public sealed class KafkaMessagePublisher(
    IProducer<string, string> producer,
    ILogger<KafkaMessagePublisher> logger) : IKafkaMessagePublisher
{
    public async Task PublishAsync(
        string topic,
        string? key,
        string payload,
        CancellationToken cancellationToken)
    {
        var result = await producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key ?? string.Empty,
                Value = payload,
            },
            cancellationToken);

        logger.LogInformation(
            "Published Kafka message to topic {Topic} at {TopicPartitionOffset}",
            topic,
            result.TopicPartitionOffset);
    }
}
