using Telegram.Bot;
using Telegram.Bot.Types;
using LinkTracker.Bot.Services;

namespace LinkTracker.Bot.Commands;

public class TrackCommand(UserStateService stateService) : IBotCommand
{
    public string Name => "/track";
    public string Description => "Add a new link to track";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
    {
        var session = stateService.GetSession(msg.Chat.Id);
        session.State = UserState.TrackAwaitingUrl;
        await bot.SendMessage(msg.Chat.Id, "Please send the URL:", cancellationToken: ct);
    }
}