using System.Collections.Concurrent;

namespace LinkTracker.Bot.Services.Notifications;

public sealed class InMemoryNotificationDeduplicationStore : INotificationDeduplicationStore
{
    private readonly ConcurrentDictionary<string, byte> _processedMessages = new();

    public bool IsProcessed(string fingerprint)
    {
        return _processedMessages.ContainsKey(fingerprint);
    }

    public void MarkProcessed(string fingerprint)
    {
        _processedMessages.TryAdd(fingerprint, 0);
    }
}
