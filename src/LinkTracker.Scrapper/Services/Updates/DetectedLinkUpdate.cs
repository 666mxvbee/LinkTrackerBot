namespace LinkTracker.Scrapper.Services.Updates;

public record DetectedLinkUpdate(
    string Url,
    string Kind,
    string Title,
    string UserName,
    DateTimeOffset CreatedAt,
    string Preview);