using System.Text.Json;

namespace LinkTracker.Scrapper.Clients;

public class StackOverflowClient(HttpClient httpClient, ILogger<StackOverflowClient> logger)
{
    public async Task<DateTimeOffset?> GetLastUpdate(long questionId)
    {
        try
        {
            var url = $"https://api.stackexchange.com/2.3/questions/{questionId}?site=stackoverflow";
            
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("StackOverflow API made error {Code} for {Id}", response.StatusCode, questionId);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            if (json.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
            {
                var firstItem = items[0];
                if (firstItem.TryGetProperty("last_activity_date", out var dateProp))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(dateProp.GetInt64());
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error StackOverflow API");
        }

        return null;
    }
}