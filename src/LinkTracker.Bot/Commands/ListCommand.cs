using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using LinkTracker.Bot.Clients;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Bot.Commands;

public class ListCommand(IScrapperClient scrapper, ILogger<ListCommand> logger) : IBotCommand
{
    public string Name => "/list";
    public string Description => "Show all tracked links";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
    {
        var chatId = msg.Chat.Id;

        try
        {
            var links = await scrapper.GetLinks(chatId);
            var linkList = links.ToList();

            if (linkList.Count == 0)
            {
                await bot.SendMessage(chatId, "You are not tracking any links yet. Use /track to add one!", cancellationToken: ct);
                return;
            }

            var messageText = "📋 *Your tracked links:*\n\n" + string.Join("\n", linkList.Select((l, i) => $"{i + 1}. {l.Url}"));

            await bot.SendMessage(
                chatId: chatId,
                text: messageText,
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching links for chat {ChatId}", chatId);
            await bot.SendMessage(chatId, "❌ Sorry, I couldn't fetch your links right now. Try again later.", cancellationToken: ct);
        }
    }
}