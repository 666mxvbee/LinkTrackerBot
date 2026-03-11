using Telegram.Bot;
using Telegram.Bot.Types;
using LinkTracker.Bot.Clients;

namespace LinkTracker.Bot.Commands;

public class StartCommand(IScrapperClient scrapper) : IBotCommand
{
    public string Name => "/start";
    public string Description => "Start the bot and register";
    
    public async Task ExecuteAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
    {
        await scrapper.RegisterChat(msg.Chat.Id);
        await bot.SendMessage(msg.Chat.Id, "Welcome! Use /help to see all the commands or /track to add a link.", cancellationToken: ct);
    }
}