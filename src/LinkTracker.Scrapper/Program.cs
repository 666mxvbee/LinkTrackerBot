using LinkTracker.Scrapper.Clients;
using LinkTracker.Scrapper.Configuration;
using LinkTracker.Scrapper.Database;
using LinkTracker.Scrapper.Jobs;
using LinkTracker.Scrapper.Repositories;
using LinkTracker.Scrapper.Repositories.Orm;
using LinkTracker.Scrapper.Repositories.Sql;
using LinkTracker.Scrapper.Services.Notifications;
using LinkTracker.Scrapper.Services.Updates;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using Npgsql;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));

builder.Services.Configure<ScrapperOptions>(
    builder.Configuration.GetSection(ScrapperOptions.SectionName));

var databaseOptions = builder.Configuration
    .GetSection(DatabaseOptions.SectionName)
    .Get<DatabaseOptions>() ?? new DatabaseOptions();

var scrapperOptions = builder.Configuration
    .GetSection(ScrapperOptions.SectionName)
    .Get<ScrapperOptions>() ?? new ScrapperOptions();

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(databaseOptions.ConnectionString));

builder.Services.AddDbContext<LinkTrackerDbContext>(options =>
    options.UseNpgsql(databaseOptions.ConnectionString));

if (databaseOptions.AccessType.Equals("ORM", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<ILinkRepository, OrmLinkRepository>();
    builder.Services.AddScoped<ITagRepository, OrmTagRepository>();
}
else
{
    builder.Services.AddScoped<ILinkRepository, SqlLinkRepository>();
    builder.Services.AddScoped<ITagRepository, SqlTagRepository>();
}

builder.Services.AddHttpClient<GitHubClient>(client =>
{
    client.BaseAddress = new Uri(scrapperOptions.GitHubBaseUrl);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("LinkTrackerBot/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

    if (!string.IsNullOrWhiteSpace(scrapperOptions.GitHubToken))
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", scrapperOptions.GitHubToken);
    }
});

builder.Services.AddHttpClient<StackOverflowClient>(client =>
{
    client.BaseAddress = new Uri(scrapperOptions.StackOverflowBaseUrl);
});

builder.Services.AddScoped<ILinkUpdateChecker>(provider =>
    provider.GetRequiredService<GitHubClient>());

builder.Services.AddScoped<ILinkUpdateChecker>(provider =>
    provider.GetRequiredService<StackOverflowClient>());

builder.Services.AddHttpClient("BotClient", client =>
{
    var botUrl = builder.Configuration["BotUrl"] ?? "http://localhost:5100";
    client.BaseAddress = new Uri(botUrl);
});

builder.Services.AddScoped<IMessageSender, HttpMessageSender>();
builder.Services.AddScoped<LinkUpdateProcessor>();

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("LinkUpdaterJob");

    q.AddJob<LinkUpdaterJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("LinkUpdaterJob-trigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInSeconds(Math.Max(1, scrapperOptions.CheckIntervalSeconds))
            .RepeatForever()));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

MigrationRunner.Run(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();