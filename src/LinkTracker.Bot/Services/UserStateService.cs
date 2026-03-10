namespace LinkTracker.Bot.Services;

public enum UserState
{
    Idle,
    TrackAwaitingUrl,
    TrackAwaitingTags,
    UntrackAwaitingUrl,
}

public class UserSession
{
    public UserState State { get; set; } = UserState.Idle;
    public string? TempUrl { get; set; }
}

public class UserStateService
{
    private readonly Dictionary<long, UserSession> _sessions = new();

    public UserSession GetSession(long chatId)
    {
        if (!_sessions.TryGetValue(chatId, out var session))
        {
            session = new UserSession();
            _sessions[chatId] = session;
        }
        return session;
    }

    public void ResetSession(long chatId)
    {
        _sessions[chatId] = new UserSession();
    }
}