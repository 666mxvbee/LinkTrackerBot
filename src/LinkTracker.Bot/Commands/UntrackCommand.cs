using Telegram.Bot;
using Telegram.Bot.Types;
using LinkTracker.Bot.Services;

namespace LinkTracker.Bot.Commands;

public class UntrackCommand(UserStateService stateService) : IBotCommand
{
    public string Name => "/untrack";
    public string Description => "Remove a link from tracking";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
    {
        var session = stateService.GetSession(msg.Chat.Id);
        session.State = UserState.UntrackAwaitingUrl;

        await bot.SendMessage(
            msg.Chat.Id,
            "Please send the URL you want to stop tracking:",
            cancellationToken: ct
        );
    }
}