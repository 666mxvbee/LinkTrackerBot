using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Commands;

public class HelpCommand : IBotCommand
{
    public string Name => "/help";
    public string Description => "Show help message";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken ct)
    {
        var text = "Available commands:\n" +
                   "/start - Register in the system\n" +
                   "/track - Start tracking a new link\n" +
                   "/list - Show your tracked links\n" +
                   "/help - Show this message";
        
        await botClient.SendMessage(message.Chat.Id, text, cancellationToken: ct);
    }
}