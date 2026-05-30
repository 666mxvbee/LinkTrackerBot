using System.Text.Json;
using LinkTracker.Scrapper.Services.Updates;

namespace LinkTracker.Scrapper.Clients;

public class StackOverflowClient(HttpClient httpClient) : ILinkUpdateChecker
{
    public bool CanHandle(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Host.Contains("stackoverflow.com", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<DetectedLinkUpdate>> CheckUpdatesAsync(
        string url,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        if (!TryParseQuestionId(url, out var questionId))
        {
            return [];
        }

        var title = await GetQuestionTitle(questionId, cancellationToken);

        var answers = await FetchQuestionItems(questionId, "answers", "Answer", title, since, cancellationToken);
        var questionComments = await FetchQuestionItems(
            questionId,
            "comments",
            "Question comment",
            title,
            since,
            cancellationToken);
        var answerComments = await FetchAnswerComments(questionId, title, since, cancellationToken);

        return answers
            .Concat(questionComments)
            .Concat(answerComments)
            .OrderBy(update => update.CreatedAt)
            .ToArray();
    }

    private async Task<string> GetQuestionTitle(long questionId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"questions/{questionId}?site=stackoverflow",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var items = json.GetProperty("items");

        return items.GetArrayLength() == 0
            ? $"StackOverflow question {questionId}"
            : items[0].GetProperty("title").GetString() ?? $"StackOverflow question {questionId}";
    }

    private async Task<IReadOnlyList<DetectedLinkUpdate>> FetchQuestionItems(
        long questionId,
        string resource,
        string kind,
        string title,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"questions/{questionId}/{resource}?site=stackoverflow&pagesize=100&order=desc&sort=creation&filter=withbody",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var updates = new List<DetectedLinkUpdate>();

        foreach (var item in json.GetProperty("items").EnumerateArray())
        {
            AddUpdateIfNew(updates, item, questionId, kind, title, since);
        }

        return updates;
    }

    private async Task<IReadOnlyList<DetectedLinkUpdate>> FetchAnswerComments(
        long questionId,
        string title,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        var answerIds = await GetAnswerIds(questionId, cancellationToken);

        if (answerIds.Length == 0)
        {
            return [];
        }

        var ids = string.Join(';', answerIds);

        using var response = await httpClient.GetAsync(
            $"answers/{ids}/comments?site=stackoverflow&pagesize=100&order=desc&sort=creation&filter=withbody",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var updates = new List<DetectedLinkUpdate>();

        foreach (var item in json.GetProperty("items").EnumerateArray())
        {
            AddUpdateIfNew(updates, item, questionId, "Answer comment", title, since);
        }

        return updates;
    }

    private async Task<long[]> GetAnswerIds(long questionId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"questions/{questionId}/answers?site=stackoverflow&pagesize=100&order=desc&sort=creation",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        return json.GetProperty("items")
            .EnumerateArray()
            .Where(item => item.TryGetProperty("answer_id", out _))
            .Select(item => item.GetProperty("answer_id").GetInt64())
            .ToArray();
    }

    private static void AddUpdateIfNew(
        ICollection<DetectedLinkUpdate> updates,
        JsonElement item,
        long questionId,
        string kind,
        string title,
        DateTimeOffset since)
    {
        var createdAt = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("creation_date").GetInt64());

        if (createdAt <= since)
        {
            return;
        }

        updates.Add(new DetectedLinkUpdate(
            Url: $"https://stackoverflow.com/questions/{questionId}",
            Kind: kind,
            Title: title,
            UserName: GetOwnerName(item),
            CreatedAt: createdAt,
            Preview: PreviewBuilder.Build(GetStringOrDefault(item, "body"))));
    }

    private static string GetOwnerName(JsonElement item)
    {
        return item.TryGetProperty("owner", out var owner)
            && owner.TryGetProperty("display_name", out var displayName)
            ? displayName.GetString() ?? "unknown"
            : "unknown";
    }

    private static string? GetStringOrDefault(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var property)
            ? property.GetString()
            : null;
    }

    private static bool TryParseQuestionId(string url, out long questionId)
    {
        questionId = 0;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var index = Array.IndexOf(parts, "questions");

        return index >= 0
            && parts.Length > index + 1
            && long.TryParse(parts[index + 1], out questionId);
    }
}
