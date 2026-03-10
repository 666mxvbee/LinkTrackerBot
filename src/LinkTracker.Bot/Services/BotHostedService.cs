using LinkTracker.Bot.Commands;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LinkTracker.Bot.Services;

public class BotHostedService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly CommandDispatcher _dispatcher;
    private readonly IEnumerable<IBotCommand> _commands;
    private readonly ILogger<BotHostedService> _logger;

    public BotHostedService(
        ITelegramBotClient botClient,
        CommandDispatcher dispatcher,
        IEnumerable<IBotCommand> commands,
        ILogger<BotHostedService> logger)
    {
        _botClient = botClient;
        _dispatcher = dispatcher;
        _commands = commands;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botCommands = _commands.Select(c => new BotCommand
        {
            Command = c.Name.TrimStart('/'), 
            Description = c.Description
        }).ToList();

        try 
        {
            await _botClient.SetMyCommands(botCommands, cancellationToken: stoppingToken);
            _logger.LogInformation("Bot commands registered successfully in Telegram menu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set bot commands.");
        }

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
        };
        
        await _botClient.ReceiveAsync(
            updateHandler: HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: { } } message)
            return;

        _logger.LogInformation("Received message from {ChatId}: {Text}", message.Chat.Id, message.Text);
        
        await _dispatcher.HandleMessageAsync(botClient, message, cancellationToken);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error occurred during bot polling.");
        return Task.CompletedTask;
    }
}