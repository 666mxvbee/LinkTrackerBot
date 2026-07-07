using LinkTracker.Shared.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace LinkTracker.Bot.Services.Notifications;

public class TelegramLinkUpdateHandler(
    ITelegramBotClient botClient,
    ILogger<TelegramLinkUpdateHandler> logger) : ILinkUpdateHandler
{
    public async Task HandleAsync(LinkUpdate update, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Sending link update notification for URL {Url} to {ChatCount} chats",
            update.Url,
            update.TgChatIds.Length);

        foreach (var chatId in update.TgChatIds)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: BuildMessage(update),
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
    }

    private static string BuildMessage(LinkUpdate update)
    {
        return $"🔔 *Update found!*\n\n" +
               $"Source: {update.Url}\n" +
               $"Description: {update.Description}";
    }
}
