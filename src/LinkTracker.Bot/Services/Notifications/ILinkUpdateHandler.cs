using LinkTracker.Shared.Models;

namespace LinkTracker.Bot.Services.Notifications;

public interface ILinkUpdateHandler
{
    Task HandleAsync(LinkUpdate update, CancellationToken cancellationToken);
}
