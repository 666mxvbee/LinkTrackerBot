using Quartz;
using LinkTracker.Scrapper.Repositories;
using LinkTracker.Scrapper.Clients;
using LinkTracker.Scrapper.Jobs;
using LinkTracker.Scrapper.Configuration;
using LinkTracker.Scrapper.Database;
using LinkTracker.Scrapper.Repositories.Sql;
using LinkTracker.Scrapper.Repositories.Orm;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));

var databaseOptions = builder.Configuration
    .GetSection(DatabaseOptions.SectionName)
    .Get<DatabaseOptions>() ?? new DatabaseOptions();

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

builder.Services.AddHttpClient<GitHubClient>();
builder.Services.AddHttpClient<StackOverflowClient>();

builder.Services.AddHttpClient("BotClient", client =>
{
    var botUrl = builder.Configuration["BotUrl"] ?? "http://localhost:5100";
    client.BaseAddress = new Uri(botUrl);
});

builder.Services.AddQuartz(q =>
{
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

MigrationRunner.Run(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
