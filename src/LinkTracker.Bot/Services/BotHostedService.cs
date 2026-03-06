using LinkTracker.Bot.Commands;
using LinkTracker.Bot.Configuration;
using Microsoft.Extensions.Options;
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
        IOptions<BotOptions> options,
        CommandDispatcher dispatcher,
        IEnumerable<IBotCommand> commands,
        ILogger<BotHostedService> logger)
    {
        if (string.IsNullOrWhiteSpace(options.Value.BotToken))
        {
            throw new InvalidOperationException("Bot token is not configured.");
        }
        
        _botClient = new TelegramBotClient(options.Value.BotToken);
        _dispatcher = dispatcher;
        _commands = commands;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botCommands = _commands.Select(c => new BotCommand
        {
            Command = c.Command.TrimStart('/'),
            Description = c.Description
        }).ToList();
        
        await _botClient.SetMyCommands(botCommands, cancellationToken: stoppingToken);
        _logger.LogInformation("Bot commands registered successfully.");
        
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
        };
        
        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
        
        _logger.LogInformation("LinkTracker Bot started receiving updates.");
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message) return;
        
        await _dispatcher.HandleMessageAsync(botClient, message, cancellationToken);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error occurred during bot polling.");
        return Task.CompletedTask;
    }
}