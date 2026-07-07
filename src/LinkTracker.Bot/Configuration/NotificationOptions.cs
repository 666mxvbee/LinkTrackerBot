namespace LinkTracker.Bot.Configuration;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public string Transport { get; init; } = NotificationTransports.Http;

    public KafkaConsumerOptions Kafka { get; init; } = new();
}

public static class NotificationTransports
{
    public const string Http = "HTTP";

    public const string Kafka = "Kafka";
}

public sealed class KafkaConsumerOptions
{
    public string BootstrapServers { get; init; } = "localhost:9092";

    public string Topic { get; init; } = "link-updates";

    public string DlqTopic { get; init; } = "link-updates-dlq";

    public string GroupId { get; init; } = "linktracker-bot";

    public int MaxRetryAttempts { get; init; } = 3;

    public int RetryDelayMilliseconds { get; init; } = 500;
}
