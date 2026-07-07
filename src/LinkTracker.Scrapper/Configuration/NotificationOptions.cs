namespace LinkTracker.Scrapper.Configuration;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public string Transport { get; init; } = NotificationTransports.Http;

    public KafkaProducerOptions Kafka { get; init; } = new();
}

public static class NotificationTransports
{
    public const string Http = "HTTP";

    public const string Kafka = "Kafka";
}

public sealed class KafkaProducerOptions
{
    public string BootstrapServers { get; init; } = "localhost:9092";

    public string Topic { get; init; } = "link-updates";

    public string DlqTopic { get; init; } = "link-updates-dlq";

    public int LingerMs { get; init; } = 10;

    public int OutboxBatchSize { get; init; } = 100;

    public int OutboxDispatchIntervalSeconds { get; init; } = 5;
}
