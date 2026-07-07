namespace LinkTracker.Scrapper.Services.Notifications;

public interface IKafkaMessagePublisher
{
    Task PublishAsync(
        string topic,
        string? key,
        string payload,
        CancellationToken cancellationToken);
}
