namespace LinkTracker.Scrapper.Services.Updates;

public static class UpdateMessageFormatter
{
    public static string Format(DetectedLinkUpdate update)
    {
        return $"""
                Type: {update.Kind}
                Title: {update.Title}
                User: {update.UserName}
                Created at: {update.CreatedAt:u}
                Preview: {update.Preview}
                """;
    }

    public static string FormatFailure(string url, string reason)
    {
        return $"""
                Failed to check link.
                Url: {url}
                Reason: {reason}
                """;
    }
}