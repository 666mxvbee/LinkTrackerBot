using System.Text.Json;

namespace LinkTracker.Scrapper.Clients;

public class GitHubClient(HttpClient httpClient, ILogger<GitHubClient> logger)
{
    public async Task<DateTimeOffset?> GetLastUpdate(string owner, string repo)
    {
        try
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LinkTrackerBot/1.0");
            
            var response = await httpClient.GetAsync($"https://api.github.com/repos/{owner}/{repo}");
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("GitHub API made error {Code} to {Owner}/{Repo}", response.StatusCode, owner, repo);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            if (json.TryGetProperty("pushed_at", out var dateProp))
            {
                return dateProp.GetDateTimeOffset();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error GitHub API");
        }
        
        return null;
    }
}