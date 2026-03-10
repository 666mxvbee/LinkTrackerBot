using LinkTracker.Bot.Commands;
using LinkTracker.Bot.Configuration;
using LinkTracker.Bot.Repositories;
using LinkTracker.Bot.Services;
using LinkTracker.Bot.Clients;
using Telegram.Bot;
using Refit;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<BotOptions>(builder.Configuration.GetSection(BotOptions.SectionName));

builder.Services.AddSingleton<ITelegramBotClient>(sp => 
{
    var options = sp.GetRequiredService<IOptions<BotOptions>>().Value;
    return new TelegramBotClient(options.BotToken);
});

builder.Services.AddRefitClient<IScrapperClient>()
    .ConfigureHttpClient((sp, client) => 
    {
        var options = sp.GetRequiredService<IOptions<BotOptions>>().Value;
        client.BaseAddress = new Uri(options.ScrapperUrl);
    });

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<UserStateService>();
builder.Services.AddSingleton<CommandDispatcher>();

builder.Services.AddTransient<IBotCommand, StartCommand>();
builder.Services.AddTransient<IBotCommand, HelpCommand>();
builder.Services.AddTransient<IBotCommand, TrackCommand>();
builder.Services.AddTransient<IBotCommand, ListCommand>();
builder.Services.AddTransient<IBotCommand, UntrackCommand>();

builder.Services.AddHostedService<BotHostedService>();

var app = builder.Build();

app.MapControllers(); 

app.Run();