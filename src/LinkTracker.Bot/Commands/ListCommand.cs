using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using LinkTracker.Bot.Clients;

namespace LinkTracker.Bot.Commands;

public class ListCommand(IScrapperClient scrapper, ILogger<ListCommand> logger) : IBotCommand
{
    public string Name => "/list";
    public string Description => "Show tracked links (use: /list <tag> for filter)";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
    {
        var chatId = msg.Chat.Id;
        var text = msg.Text ?? string.Empty;

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string? tagFilter = parts.Length > 1 ? parts[1].ToLower() : null;

        try
        {
            var response = await scrapper.GetLinks(chatId, tagFilter);

            if (response?.Links == null)
            {
                await bot.SendMessage(chatId, "No data received from service.", cancellationToken: ct);
                return;
            }
            
            var linkList = response.Links.ToList();

            if (linkList.Count == 0)
            {
                var emptyMsg = tagFilter == null 
                    ? "You are not tracking any links yet. Use /track to add one!" 
                    : $"No links found with tag: *{tagFilter}*";

                await bot.SendMessage(chatId, emptyMsg, parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            var header = tagFilter == null 
                ? "📋 *Your tracked links:*" 
                : $"📋 *Links with tag '{tagFilter}':*";

            var messageLines = linkList.Select((l, i) =>
            {
                var safeUrl = l.Url.Replace("_", "\\_");
                var tags = l.Tags ?? Array.Empty<string>();
                var safeTags = tags.Select(t => t.Replace("_", "\\_"));
                var tagsPart = tags.Length > 0 
                    ? $" _{string.Join(", ", safeTags)}_" 
                    : "";
                return $"{i + 1}. {safeUrl}{tagsPart}";
            });

            var messageText = $"{header}\n\n" + string.Join("\n", messageLines);

            await bot.SendMessage(
                chatId: chatId,
                text: messageText,
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ListCommand failed for chat {ChatId}. Details: {Message}", chatId, ex.ToString());
            await bot.SendMessage(chatId, "❌ Sorry, I couldn't fetch your links right now. Try again later.", cancellationToken: ct);
        }
    }
}