using LinkTracker.Shared.Models;

namespace LinkTracker.Scrapper.Services.Notifications;

public interface IMessageSender
{
    Task SendAsync(LinkUpdate update, CancellationToken cancellationToken);
}