namespace LinkTracker.Bot.Services.Notifications;

public interface INotificationDeduplicationStore
{
    bool IsProcessed(string fingerprint);

    void MarkProcessed(string fingerprint);
}
