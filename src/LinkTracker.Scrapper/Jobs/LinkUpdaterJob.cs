using LinkTracker.Scrapper.Services.Updates;
using Quartz;

namespace LinkTracker.Scrapper.Jobs;

public class LinkUpdaterJob(
    LinkUpdateProcessor processor,
    ILogger<LinkUpdaterJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Link update check started");

        await processor.ProcessAsync(context.CancellationToken);

        logger.LogInformation("Link update check finished");
    }
}