using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Commands;

public interface IBotCommand
{
    string Name { get; }
    string Description { get; }
    Task ExecuteAsync(ITelegramBotClient bot, Message msg, CancellationToken ct);
}