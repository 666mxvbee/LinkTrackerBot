using DotNet.Testcontainers.Images;
using Testcontainers.Kafka;

namespace LinkTracker.Scrapper.Tests.Kafka;

public sealed class KafkaFixture : IAsyncLifetime
{
    private readonly KafkaContainer _container = new KafkaBuilder("confluentinc/cp-kafka:7.6.1")
        .WithImagePullPolicy(PullPolicy.Missing)
        .Build();

    public string BootstrapServers => _container.GetBootstrapAddress();

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
