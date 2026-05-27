using LinkTracker.Scrapper.Database;
using LinkTracker.Scrapper.Database.Entities;
using LinkTracker.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Repositories.Orm;

public class OrmLinkRepository(LinkTrackerDbContext dbContext) : ILinkRepository
{
    public void AddChat(long chatId)
    {
        if (dbContext.Chats.Any(chat => chat.Id == chatId))
        {
            return;
        }

        dbContext.Chats.Add(new ChatEntity
        {
            Id = chatId
        });

        dbContext.SaveChanges();
    }

    public void RemoveChat(long chatId)
    {
        var chat = dbContext.Chats.FirstOrDefault(chat => chat.Id == chatId);

        if (chat is null)
        {
            return;
        }

        dbContext.Chats.Remove(chat);
        dbContext.SaveChanges();
    }

    public bool ChatExists(long chatId)
    {
        return dbContext.Chats
            .AsNoTracking()
            .Any(chat => chat.Id == chatId);
    }

    public LinkResponse? AddLink(long chatId, string url, string[]? tags)
    {
        var normalizedTags = NormalizeTags(tags);

        using var transaction = dbContext.Database.BeginTransaction();

        if (!dbContext.Chats.Any(chat => chat.Id == chatId))
        {
            return null;
        }

        var link = dbContext.Links.FirstOrDefault(link => link.Url == url);

        if (link is null)
        {
            link = new LinkEntity
            {
                Url = url,
                LastCheckedAt = DateTimeOffset.UtcNow
            };

            dbContext.Links.Add(link);
            dbContext.SaveChanges();
        }

        var subscriptionExists = dbContext.ChatLinks.Any(chatLink =>
            chatLink.ChatId == chatId && chatLink.LinkId == link.Id);

        if (subscriptionExists)
        {
            return null;
        }

        dbContext.ChatLinks.Add(new ChatLinkEntity
        {
            ChatId = chatId,
            LinkId = link.Id
        });

        dbContext.SaveChanges();

        foreach (var tagName in normalizedTags)
        {
            var tag = dbContext.Tags.FirstOrDefault(tag => tag.Name == tagName);

            if (tag is null)
            {
                tag = new TagEntity
                {
                    Name = tagName
                };

                dbContext.Tags.Add(tag);
                dbContext.SaveChanges();
            }

            dbContext.ChatLinkTags.Add(new ChatLinkTagEntity
            {
                ChatId = chatId,
                LinkId = link.Id,
                TagId = tag.Id
            });
        }

        dbContext.SaveChanges();
        transaction.Commit();

        return new LinkResponse(link.Id, link.Url, normalizedTags);
    }

    public bool RemoveLink(long chatId, string url)
    {
        var chatLink = dbContext.ChatLinks.FirstOrDefault(chatLink =>
            chatLink.ChatId == chatId && chatLink.Link.Url == url);

        if (chatLink is null)
        {
            return false;
        }

        dbContext.ChatLinks.Remove(chatLink);
        return dbContext.SaveChanges() > 0;
    }

    public IEnumerable<LinkResponse> GetLinks(long chatId, string? tag = null, int offset = 0, int limit = 100)
    {
        var query = dbContext.ChatLinks
            .AsNoTracking()
            .Where(chatLink => chatLink.ChatId == chatId);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = tag.Trim();

            query = query.Where(chatLink =>
                chatLink.ChatLinkTags.Any(chatLinkTag =>
                    EF.Functions.ILike(chatLinkTag.Tag.Name, normalizedTag)));
        }

        var chatLinks = query
            .Include(chatLink => chatLink.Link)
            .Include(chatLink => chatLink.ChatLinkTags)
            .ThenInclude(chatLinkTag => chatLinkTag.Tag)
            .OrderBy(chatLink => chatLink.CreatedAt)
            .ThenBy(chatLink => chatLink.LinkId)
            .Skip(NormalizeOffset(offset))
            .Take(NormalizeLimit(limit))
            .AsSplitQuery()
            .ToList();

        return chatLinks
            .Select(chatLink => new LinkResponse(
                chatLink.Link.Id,
                chatLink.Link.Url,
                chatLink.ChatLinkTags
                    .Select(chatLinkTag => chatLinkTag.Tag.Name)
                    .OrderBy(tagName => tagName)
                    .ToArray()))
            .ToList();
    }

    public IEnumerable<(string Url, long[] ChatIds, DateTimeOffset LastUpdate)> GetLinksForUpdate(
        int offset = 0,
        int limit = 100)
    {
        var links = dbContext.Links
            .AsNoTracking()
            .Where(link => link.ChatLinks.Any())
            .OrderBy(link => link.Id)
            .Skip(NormalizeOffset(offset))
            .Take(NormalizeLimit(limit))
            .Select(link => new
            {
                link.Id,
                link.Url,
                link.LastCheckedAt
            })
            .ToList();

        var linkIds = links.Select(link => link.Id).ToArray();

        var chatIdsByLinkId = dbContext.ChatLinks
            .AsNoTracking()
            .Where(chatLink => linkIds.Contains(chatLink.LinkId))
            .Select(chatLink => new
            {
                chatLink.LinkId,
                chatLink.ChatId
            })
            .ToList()
            .GroupBy(chatLink => chatLink.LinkId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(chatLink => chatLink.ChatId)
                    .OrderBy(chatId => chatId)
                    .ToArray());

        return links
            .Select(link => (
                link.Url,
                chatIdsByLinkId.GetValueOrDefault(link.Id, Array.Empty<long>()),
                link.LastCheckedAt))
            .ToList();
    }

    public void UpdateLastCheckTime(string url, DateTimeOffset lastUpdate)
    {
        var link = dbContext.Links.FirstOrDefault(link => link.Url == url);

        if (link is null)
        {
            return;
        }

        link.LastCheckedAt = lastUpdate.ToUniversalTime();
        dbContext.SaveChanges();
    }

    private static string[] NormalizeTags(string[]? tags)
    {
        return tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
    }

    private static int NormalizeOffset(int offset)
    {
        return Math.Max(0, offset);
    }

    private static int NormalizeLimit(int limit)
    {
        return Math.Clamp(limit, 1, 1000);
    }
}