using LinkTracker.Bot.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Services;

public class CommandDispatcher
{
    private readonly IEnumerable<IBotCommand> _commands;
    private readonly ILogger<CommandDispatcher> _logger;

    public CommandDispatcher(IEnumerable<IBotCommand> commands, ILogger<CommandDispatcher> logger)
    {
        _commands = commands;
        _logger = logger;
    }

    public async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.Text is not { } messageText) return;
        
        var commandName = messageText.Split(' ').First();
        var command = _commands.FirstOrDefault(c => c.Command.Equals(commandName, StringComparison.OrdinalIgnoreCase));

        if (command != null)
        {
            _logger.LogInformation("Executing command {CommandName} for user {UserId}", commandName, message.From?.Id);
            await command.ExecuteAsync(botClient, message, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Unknown command {CommandName} from user {UserId}", commandName, message.From?.Id);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Unknown command. Use /help to see the list of commands.",
                cancellationToken: cancellationToken);
        }
    }
}