namespace LinkTracker.Bot.Repositories;

public interface IUserRepository
{
    Task AddUserAsync(long userId, string username, CancellationToken cancellationToken);
    Task<bool> UserExistsAsync(long userId, CancellationToken cancellationToken);
}