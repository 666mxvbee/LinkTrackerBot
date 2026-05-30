using LinkTracker.Scrapper.Clients;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace LinkTracker.Scrapper.Tests.Updates;

public class StackOverflowClientTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    [Fact]
    public async Task CheckUpdatesAsync_ReturnsAnswersQuestionCommentsAndAnswerComments()
    {
        _server
            .Given(Request.Create().WithPath("/questions/123").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                      "items": [
                        { "title": "How to test StackOverflow updates?" }
                      ]
                    }
                    """));

        _server
            .Given(Request.Create().WithPath("/questions/123/answers").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                      "items": [
                        {
                          "answer_id": 777,
                          "creation_date": 1767268800,
                          "body": "<p>Answer body</p>",
                          "owner": { "display_name": "answer-user" }
                        }
                      ]
                    }
                    """));

        _server
            .Given(Request.Create().WithPath("/questions/123/comments").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                      "items": [
                        {
                          "creation_date": 1767272400,
                          "body": "<p>Question comment</p>",
                          "owner": { "display_name": "question-comment-user" }
                        }
                      ]
                    }
                    """));

        _server
            .Given(Request.Create().WithPath("/answers/777/comments").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                      "items": [
                        {
                          "creation_date": 1767276000,
                          "body": "<p>Answer comment</p>",
                          "owner": { "display_name": "answer-comment-user" }
                        }
                      ]
                    }
                    """));

        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.Url!) };
        var client = new StackOverflowClient(httpClient);

        var updates = await client.CheckUpdatesAsync(
            "https://stackoverflow.com/questions/123/how-to-test",
            DateTimeOffset.FromUnixTimeSeconds(1767260000),
            CancellationToken.None);

        Assert.Equal(3, updates.Count);
        Assert.Contains(updates, update => update.Kind == "Answer" && update.UserName == "answer-user");
        Assert.Contains(updates, update => update.Kind == "Question comment" && update.Preview == "Question comment");
        Assert.Contains(updates, update => update.Kind == "Answer comment" && update.UserName == "answer-comment-user");
        Assert.All(updates, update => Assert.Equal("How to test StackOverflow updates?", update.Title));
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}
