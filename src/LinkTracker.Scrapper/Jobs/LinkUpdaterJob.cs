using Quartz;
using LinkTracker.Scrapper.Repositories;
using System.Net.Http;
using LinkTracker.Scrapper.Clients;
using LinkTracker.Shared.Models;

namespace LinkTracker.Scrapper.Jobs;

public class LinkUpdaterJob(
    ILinkRepository repo,
    IHttpClientFactory httpClientFactory,
    GitHubClient github,
    StackOverflowClient stackOverflow,
    ILogger<LinkUpdaterJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var botClient = httpClientFactory.CreateClient("BotClient");

        foreach (var (url, chatIds, lastUpdate) in repo.GetLinksForUpdate())
        {
            if (chatIds.Length == 0)
            {
                continue;
            }

            DateTimeOffset? currentUpdateFromApi = null;

            try
            {
                if (url.Contains("github.com"))
                {
                    var parts = url.TrimEnd('/').Split('/');
                    if (parts.Length >= 2)
                    {
                        currentUpdateFromApi = await github.GetLastUpdate(parts[^2], parts[^1]);
                    }
                }
                else if (url.Contains("stackoverflow.com"))
                {
                    var parts = url.Split('/');
                    var questionsIndex = Array.IndexOf(parts, "questions");
                    if (questionsIndex != -1 && parts.Length > questionsIndex + 1)
                    {
                        if (long.TryParse(parts[questionsIndex + 1], out var questionId))
                        {
                            currentUpdateFromApi = await stackOverflow.GetLastUpdate(questionId);
                        }
                    }
                }

                if (currentUpdateFromApi.HasValue && currentUpdateFromApi > lastUpdate)
                {
                    logger.LogInformation("Found a new update for {Url}. Was: {Old}, Now: {New}", url, lastUpdate, currentUpdateFromApi);

                    var updateReq = new LinkUpdate(
                        Id: 0,
                        Url: url,
                        Description: $"there is a new activity in repo or in question! (Date: {currentUpdateFromApi:g})", TgChatIds: chatIds);

                    var response = await botClient.PostAsJsonAsync("/updates", updateReq);

                    if (response.IsSuccessStatusCode)
                    {
                        repo.UpdateLastCheckTime(url, currentUpdateFromApi.Value);
                    }
                    else
                    {
                        logger.LogWarning("Bot disable notification for {Url}. Code: {Code}", url, response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while checking links {Url}", url);
            }
        }
    }
}