using LinkTracker.Scrapper.Clients;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace LinkTracker.Scrapper.Tests.Updates;

public class GitHubClientTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    [Fact]
    public async Task CheckUpdatesAsync_ReturnsNewIssueAndPullRequestWithRequiredFields()
    {
        _server
            .Given(Request.Create().WithPath("/repos/octo/demo/issues").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""
                    [
                      {
                        "html_url": "https://github.com/octo/demo/issues/1",
                        "title": "New issue",
                        "created_at": "2026-01-02T12:00:00Z",
                        "body": "{{new string('i', 250)}}",
                        "user": { "login": "issue-author" }
                      },
                      {
                        "html_url": "https://github.com/octo/demo/pull/2",
                        "title": "PR hidden in issues endpoint",
                        "created_at": "2026-01-02T13:00:00Z",
                        "body": "skip",
                        "user": { "login": "pull-author" },
                        "pull_request": {}
                      }
                    ]
                    """));

        _server
            .Given(Request.Create().WithPath("/repos/octo/demo/pulls").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    [
                      {
                        "html_url": "https://github.com/octo/demo/pull/2",
                        "title": "New PR",
                        "created_at": "2026-01-03T12:00:00Z",
                        "body": "Pull request description",
                        "user": { "login": "pull-author" }
                      }
                    ]
                    """));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Url!) };
        var client = new GitHubClient(httpClient);

        var updates = await client.CheckUpdatesAsync(
            "https://github.com/octo/demo",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            CancellationToken.None);

        Assert.Equal(2, updates.Count);

        var issue = Assert.Single(updates, update => update.Kind == "Issue");
        Assert.Equal("New issue", issue.Title);
        Assert.Equal("issue-author", issue.UserName);
        Assert.Equal(200, issue.Preview.Length);

        var pullRequest = Assert.Single(updates, update => update.Kind == "Pull request");
        Assert.Equal("New PR", pullRequest.Title);
        Assert.Equal("pull-author", pullRequest.UserName);
        Assert.Contains("Pull request description", pullRequest.Preview);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}
