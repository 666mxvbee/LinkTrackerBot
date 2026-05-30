using LinkTracker.Shared.Models;

namespace LinkTracker.Scrapper.Services.Notifications;

public class HttpMessageSender(IHttpClientFactory httpClientFactory) : IMessageSender
{
    public async Task SendAsync(LinkUpdate update, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("BotClient");

        var response = await client.PostAsJsonAsync("/updates", update, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}