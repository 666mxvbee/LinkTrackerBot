using System.Collections.Concurrent;

namespace LinkTracker.Bot.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<long, string> _users = new();

    public Task AddUserAsync(long userId, string username, CancellationToken cancellationToken)
    {
        _users.TryAdd(userId, username);
        return Task.CompletedTask;
    }

    public Task<bool> UserExistsAsync(long userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_users.ContainsKey(userId));
    }
}