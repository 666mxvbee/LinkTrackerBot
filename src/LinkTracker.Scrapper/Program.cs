using Quartz;
using LinkTracker.Scrapper.Repositories;
using LinkTracker.Scrapper.Clients;
using LinkTracker.Scrapper.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSingleton<ILinkRepository, InMemoryLinkRepository>();

builder.Services.AddHttpClient<GitHubClient>();
builder.Services.AddHttpClient<StackOverflowClient>();

builder.Services.AddHttpClient("BotClient", client => {
    var botUrl = builder.Configuration["BotUrl"] ?? "http://localhost:5100";
    client.BaseAddress = new Uri(botUrl);
});

builder.Services.AddQuartz(q => {
    var jobKey = new JobKey("LinkUpdaterJob");
    
    q.AddJob<LinkUpdaterJob>(opts => opts.WithIdentity(jobKey));
    
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("LinkUpdaterJob-trigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInSeconds(30)
            .RepeatForever()));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();