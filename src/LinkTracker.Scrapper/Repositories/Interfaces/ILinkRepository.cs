using LinkTracker.Shared.Models;

namespace LinkTracker.Scrapper.Repositories;

public interface ILinkRepository
{
    void AddChat(long chatId);
    void RemoveChat(long chatId);
    bool ChatExists(long chatId);
    LinkResponse? AddLink(long chatId, string url, string[]? tags);
    bool RemoveLink(long chatId, string url);
    IEnumerable<LinkResponse> GetLinks(long chatId, string? tag = null, int offset = 0, int limit = 100);
    IEnumerable<(string Url, long[] ChatIds, DateTimeOffset LastUpdate)> GetLinksForUpdate(int offset = 0, int limit = 100);
    void UpdateLastCheckTime(string url, DateTimeOffset lastUpdate);
}