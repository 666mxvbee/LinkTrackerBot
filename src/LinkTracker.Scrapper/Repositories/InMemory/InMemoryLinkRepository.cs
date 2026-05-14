using System.Collections.Concurrent;
using LinkTracker.Shared.Models;

namespace LinkTracker.Scrapper.Repositories;

public class InMemoryLinkRepository : ILinkRepository
{
    private readonly ConcurrentDictionary<long, List<LinkResponse>> _userLinks = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _linkMetadata = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _linkIdsByUrl = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    private long _nextLinkId;

    public void AddChat(long chatId) => _userLinks.TryAdd(chatId, new List<LinkResponse>());

    public void RemoveChat(long chatId) => _userLinks.TryRemove(chatId, out _);

    public bool ChatExists(long chatId) => _userLinks.ContainsKey(chatId);

    public LinkResponse? AddLink(long chatId, string url, string[]? tags)
    {
        lock (_lock)
        {
            if (!_userLinks.TryGetValue(chatId, out var links))
            {
                return null;
            }

            if (links.Any(link => string.Equals(link.Url, url, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var linkId = _linkIdsByUrl.GetOrAdd(url, _ => Interlocked.Increment(ref _nextLinkId));
            var link = new LinkResponse(linkId, url, tags ?? Array.Empty<string>());

            links.Add(link);
            _linkMetadata.TryAdd(url, DateTimeOffset.UtcNow);

            return link;
        }
    }

    public bool RemoveLink(long chatId, string url)
    {
        lock (_lock)
        {
            return _userLinks.TryGetValue(chatId, out var links)
                && links.RemoveAll(link => string.Equals(link.Url, url, StringComparison.OrdinalIgnoreCase)) > 0;
        }
    }

    public IEnumerable<LinkResponse> GetLinks(long chatId, string? tag = null, int offset = 0, int limit = 100)
    {
        lock (_lock)
        {
            if (!_userLinks.TryGetValue(chatId, out var links))
            {
                return Enumerable.Empty<LinkResponse>();
            }

            var query = links.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(tag))
            {
                query = query.Where(link =>
                    link.Tags.Any(linkTag => string.Equals(linkTag, tag, StringComparison.OrdinalIgnoreCase)));
            }

            return query
                .Skip(offset)
                .Take(limit)
                .ToList();
        }
    }

    public IEnumerable<(string Url, long[] ChatIds, DateTimeOffset LastUpdate)> GetLinksForUpdate(
        int offset = 0,
        int limit = 100)
    {
        lock (_lock)
        {
            return _linkMetadata
                .Select(metadata =>
                {
                    var url = metadata.Key;
                    var chatIds = _userLinks
                        .Where(userLinks => userLinks.Value.Any(link =>
                            string.Equals(link.Url, url, StringComparison.OrdinalIgnoreCase)))
                        .Select(userLinks => userLinks.Key)
                        .Distinct()
                        .ToArray();

                    return (Url: url, ChatIds: chatIds, LastUpdate: metadata.Value);
                })
                .Where(link => link.ChatIds.Length > 0)
                .Skip(offset)
                .Take(limit)
                .ToList();
        }
    }

    public void UpdateLastCheckTime(string url, DateTimeOffset lastUpdate)
    {
        _linkMetadata[url] = lastUpdate;
    }
}