namespace LinkTracker.Scrapper.Services.Updates;

public interface ILinkUpdateChecker
{
    bool CanHandle(string url);

    Task<IReadOnlyList<DetectedLinkUpdate>> CheckUpdatesAsync(
        string url,
        DateTimeOffset since,
        CancellationToken cancellationToken);
}