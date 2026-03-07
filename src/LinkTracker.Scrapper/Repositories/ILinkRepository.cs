using LinkTracker.Shared.Models;

namespace LinkTracker.Scrapper.Repositories;

public interface ILinkRepository
{
    void AddChat(long chatId);
    void RemoveChat(long chatId);
    bool ChatExists(long chatId);
    LinkResponse? AddLink(long chatId, string url, string[]? tags);
    bool RemoveLink(long chatId, string url);
    IEnumerable<LinkResponse> GetLinks(long chatId);
    IEnumerable<(string Url, long[] ChatIds, DateTimeOffset LastUpdate)> GetLinksForUpdate();
    void UpdateLastCheckTime(string url, DateTimeOffset lastUpdate);
}