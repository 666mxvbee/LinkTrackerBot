using LinkTracker.Bot.Commands;
using LinkTracker.Bot.Configuration;
using LinkTracker.Bot.Repositories;
using LinkTracker.Bot.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<BotOptions>(builder.Configuration.GetSection(BotOptions.SectionName));

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<CommandDispatcher>();

builder.Services.AddTransient<IBotCommand, StartCommand>();
builder.Services.AddTransient<IBotCommand, HelpCommand>();

builder.Services.AddHostedService<BotHostedService>();

var host = builder.Build();
host.Run();