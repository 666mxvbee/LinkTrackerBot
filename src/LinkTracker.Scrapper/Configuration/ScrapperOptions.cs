namespace LinkTracker.Scrapper.Configuration;

public sealed class ScrapperOptions
{
    public const string SectionName = "Scrapper";

    public int CheckIntervalSeconds { get; init; } = 30;
    public int BatchSize { get; init; } = 100;
    public int Parallelism { get; init; } = 4;

    public string GitHubBaseUrl { get; init; } = "https://api.github.com/";
    public string StackOverflowBaseUrl { get; init; } = "https://api.stackexchange.com/2.3/";
}