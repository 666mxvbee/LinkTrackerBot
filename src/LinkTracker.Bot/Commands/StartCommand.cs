using LinkTracker.Bot.Repositories;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Commands;

public class StartCommand : IBotCommand
{
    private readonly IUserRepository _userRepository;
    
    public string Command => "/start";
    public string Description => "Starts a bot.";

    public StartCommand(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        long userId = message.Chat.Id;
        string username = message.From?.Username ?? "Unknown";

        if (!await _userRepository.UserExistsAsync(userId, cancellationToken))
        {
            await _userRepository.AddUserAsync(userId, username, cancellationToken);
        }
        
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Welcome! Use /help to see all available commands.",
            cancellationToken: cancellationToken);
    }
}