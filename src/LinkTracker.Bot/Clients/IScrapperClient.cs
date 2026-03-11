using Refit;
using LinkTracker.Shared.Models;

namespace LinkTracker.Bot.Clients;

public interface IScrapperClient
{
    [Post("/tg-chat/{id}")]
    Task RegisterChat(long id);

    [Post("/links")]
    Task AddLink([Header("Tg-Chat-Id")] long chatId, [Body] AddLinkRequest request);

    [Get("/links")]
    Task<ListLinksResponse> GetLinks([Header("Tg-Chat-Id")] long chatId, [Query] string? tag = null);
    
    [Delete("/links")]
    Task<LinkResponse> RemoveLink([Header("Tg-Chat-Id")] long chatId, [Body] RemoveLinkRequest request);
}