namespace LinkTracker.Bot.Configuration;

public class BotOptions
{
    public const string SectionName = "BotConfiguration";
    public string BotToken { get; set; } = string.Empty;
    public string ScrapperUrl { get; set; } = string.Empty;
}