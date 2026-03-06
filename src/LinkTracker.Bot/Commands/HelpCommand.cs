using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Commands;

public class HelpCommand : IBotCommand
{
    public string Command => "/help";
    public string Description => "Displays help.";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var helpText = "Available commands:\n/start - Start the bot.\n/help - Shows all the commands.";
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: helpText,
            cancellationToken: cancellationToken);
    }
}