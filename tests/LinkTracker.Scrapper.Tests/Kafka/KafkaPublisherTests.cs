using Confluent.Kafka;
using LinkTracker.Scrapper.Services.Notifications;
using Microsoft.Extensions.Logging.Abstractions;

namespace LinkTracker.Scrapper.Tests.Kafka;

[Collection(KafkaCollection.Name)]
public class KafkaPublisherTests(KafkaFixture fixture)
{
    [Fact]
    public async Task KafkaMessagePublisher_PublishesMessageToTopic()
    {
        var topic = $"link-updates-{Guid.NewGuid():N}";
        await KafkaTestSupport.CreateTopicAsync(fixture.BootstrapServers, topic);

        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = fixture.BootstrapServers,
            Acks = Acks.All
        }).Build();

        var publisher = new KafkaMessagePublisher(
            producer,
            NullLogger<KafkaMessagePublisher>.Instance);

        await publisher.PublishAsync(
            topic,
            "link-42",
            """{"url":"https://github.com/octo/repo"}""",
            CancellationToken.None);

        producer.Flush(TimeSpan.FromSeconds(5));

        var consumed = KafkaTestSupport.ConsumeSingle(
            fixture.BootstrapServers,
            topic,
            TimeSpan.FromSeconds(15));

        Assert.Equal("link-42", consumed.Message.Key);
        Assert.Contains("github.com/octo/repo", consumed.Message.Value);
    }
}
