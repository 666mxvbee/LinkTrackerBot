using System.Text.Json;
using LinkTracker.Scrapper.Services.Updates;

namespace LinkTracker.Scrapper.Clients;

public class GitHubClient(HttpClient httpClient) : ILinkUpdateChecker
{
    public bool CanHandle(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Host.Contains("github.com", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<DetectedLinkUpdate>> CheckUpdatesAsync(
        string url,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        if (!TryParseRepository(url, out var owner, out var repo))
        {
            return [];
        }

        var issues = await FetchAsync(owner, repo, "issues", "Issue", since, cancellationToken);
        var pulls = await FetchAsync(owner, repo, "pulls", "Pull request", since, cancellationToken);

        return issues
            .Concat(pulls)
            .OrderBy(update => update.CreatedAt)
            .ToArray();
    }

    private async Task<IReadOnlyList<DetectedLinkUpdate>> FetchAsync(
        string owner,
        string repo,
        string resource,
        string kind,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"repos/{owner}/{repo}/{resource}?state=all&sort=created&direction=desc&per_page=100",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        var updates = new List<DetectedLinkUpdate>();

        foreach (var item in json.EnumerateArray())
        {
            if (resource == "issues" && item.TryGetProperty("pull_request", out _))
            {
                continue;
            }

            var createdAt = item.GetProperty("created_at").GetDateTimeOffset();

            if (createdAt <= since)
            {
                continue;
            }

            updates.Add(new DetectedLinkUpdate(
                Url: item.GetProperty("html_url").GetString() ?? string.Empty,
                Kind: kind,
                Title: item.GetProperty("title").GetString() ?? string.Empty,
                UserName: item.GetProperty("user").GetProperty("login").GetString() ?? "unknown",
                CreatedAt: createdAt,
                Preview: PreviewBuilder.Build(item.GetProperty("body").GetString())));
        }

        return updates;
    }

    private static bool TryParseRepository(string url, out string owner, out string repo)
    {
        owner = string.Empty;
        repo = string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            return false;
        }

        owner = parts[0];
        repo = parts[1];
        return true;
    }
}