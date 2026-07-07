using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace LinkTracker.Scrapper.Tests.Kafka;

internal static class KafkaTestSupport
{
    public static async Task CreateTopicAsync(string bootstrapServers, string topic)
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        }).Build();

        try
        {
            await admin.CreateTopicsAsync(
            [
                new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            ]);
        }
        catch (CreateTopicsException ex) when (
            ex.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
        {
        }
    }

    public static async Task ProduceAsync(
        string bootstrapServers,
        string topic,
        string key,
        string value)
    {
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All
        }).Build();

        await producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = value
            });

        producer.Flush(TimeSpan.FromSeconds(5));
    }

    public static ConsumeResult<string, string> ConsumeSingle(
        string bootstrapServers,
        string topic,
        TimeSpan timeout)
    {
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = $"test-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

        consumer.Subscribe(topic);

        var deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(500));

            if (result is not null)
            {
                return result;
            }
        }

        throw new TimeoutException($"Kafka message was not consumed from topic {topic}.");
    }
}
