using System.Collections.Concurrent;
using LinkTracker.Shared.Models;

namespace LinkTracker.Scrapper.Repositories;

public class InMemoryLinkRepository : ILinkRepository
{
    private readonly ConcurrentDictionary<long, List<LinkResponse>> _userLinks = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _linkMetadata= new();

    public void AddChat(long chatId) => _userLinks.TryAdd(chatId, new());
    public void RemoveChat(long chatId) => _userLinks.TryRemove(chatId, out _);
    public bool ChatExists(long chatId) => _userLinks.ContainsKey(chatId);

    public LinkResponse? AddLink(long chatId, string url, string[]? tags)
    {
        if (!_userLinks.ContainsKey(chatId))
        {
            return null;
        }

        if (_userLinks[chatId].Any(l => l.Url == url))
        {
            return null;
        }
        
        var link = new LinkResponse(Random.Shared.Next(1, 10000), url, tags ?? Array.Empty<string>());
        _userLinks[chatId].Add(link);
        _linkMetadata.TryAdd(url, DateTimeOffset.UtcNow);
        return link;
    }

    public bool RemoveLink(long chatId, string url) =>
        _userLinks.TryGetValue(chatId, out var links) && links.RemoveAll(l => l.Url == url) > 0;

    public IEnumerable<LinkResponse> GetLinks(long chatId) =>
        _userLinks.TryGetValue(chatId, out var links) ? links.ToList() : Enumerable.Empty<LinkResponse>();

    public IEnumerable<(string Url, long[] ChatIds, DateTimeOffset LastUpdate)> GetLinksForUpdate()
    {
        return _linkMetadata.Select(m => 
        {
            var url = m.Key;
            var lastUpdate = m.Value;

            var chatIds = _userLinks
                .Where(u => u.Value.Any(l => l.Url == url))
                .Select(u => u.Key)
                .Distinct()
                .ToArray();

            return (url, chatIds, lastUpdate);
        }).ToList();
    }
    
    public void UpdateLastCheckTime(string url, DateTimeOffset lastUpdate) => _linkMetadata[url] = lastUpdate;
}