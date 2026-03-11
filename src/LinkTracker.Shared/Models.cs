namespace LinkTracker.Shared.Models;

public record AddLinkRequest(string Link, string[]? Tags = null);
public record RemoveLinkRequest(string Link);
public record LinkResponse(long Id, string Url, string[] Tags);
public record LinkUpdate(long Id, string Url, string Description, long[] TgChatIds);
public record ListLinksResponse(LinkResponse[] Links, int Size);