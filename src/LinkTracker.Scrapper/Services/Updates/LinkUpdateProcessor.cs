using LinkTracker.Scrapper.Configuration;
using LinkTracker.Scrapper.Repositories;
using LinkTracker.Scrapper.Services.Notifications;
using LinkTracker.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Services.Updates;

public class LinkUpdateProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<ScrapperOptions> options,
    ILogger<LinkUpdateProcessor> logger)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var batchSize = Math.Clamp(options.Value.BatchSize, 50, 500);
        var parallelism = Math.Max(1, options.Value.Parallelism);

        for (var offset = 0; !cancellationToken.IsCancellationRequested; offset += batchSize)
        {
            var batch = GetBatch(offset, batchSize);

            if (batch.Length == 0)
            {
                break;
            }

            await ProcessBatchAsync(batch, parallelism, cancellationToken);

            if (batch.Length < batchSize)
            {
                break;
            }
        }
    }

    private (string Url, long[] ChatIds, DateTimeOffset LastUpdate)[] GetBatch(int offset, int batchSize)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILinkRepository>();

        return repository.GetLinksForUpdate(offset, batchSize).ToArray();
    }

    private async Task ProcessBatchAsync(
        IEnumerable<(string Url, long[] ChatIds, DateTimeOffset LastUpdate)> batch,
        int parallelism,
        CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(parallelism);

        var tasks = batch
            .Select(link => ProcessLinkWithSemaphoreAsync(link, semaphore, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task ProcessLinkWithSemaphoreAsync(
        (string Url, long[] ChatIds, DateTimeOffset LastUpdate) link,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            await ProcessLinkAsync(link.Url, link.ChatIds, link.LastUpdate, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task ProcessLinkAsync(
        string url,
        long[] chatIds,
        DateTimeOffset lastUpdate,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ILinkRepository>();
            var checkers = scope.ServiceProvider.GetServices<ILinkUpdateChecker>();
            var sender = scope.ServiceProvider.GetRequiredService<IMessageSender>();

            var checker = checkers.FirstOrDefault(checker => checker.CanHandle(url));

            if (checker is null)
            {
                logger.LogWarning("Unsupported link type: {Url}", url);
                await SendFailureReport(sender, url, chatIds, "Unsupported link type", cancellationToken);
                repository.UpdateLastCheckTime(url, DateTimeOffset.UtcNow);
                return;
            }

            var updates = await checker.CheckUpdatesAsync(url, lastUpdate, cancellationToken);

            foreach (var update in updates)
            {
                await sender.SendAsync(new LinkUpdate(
                    Id: 0,
                    Url: url,
                    Description: UpdateMessageFormatter.Format(update),
                    TgChatIds: chatIds), cancellationToken);
            }

            repository.UpdateLastCheckTime(url, DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process link {Url}", url);

            await TrySendFailureReport(url, chatIds, ex.Message, cancellationToken);
        }
    }

    private async Task TrySendFailureReport(
        string url,
        long[] chatIds,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<IMessageSender>();

            await SendFailureReport(sender, url, chatIds, reason, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send failure report for {Url}", url);
        }
    }

    private static Task SendFailureReport(
        IMessageSender sender,
        string url,
        long[] chatIds,
        string reason,
        CancellationToken cancellationToken)
    {
        return sender.SendAsync(new LinkUpdate(
            Id: 0,
            Url: url,
            Description: UpdateMessageFormatter.FormatFailure(url, reason),
            TgChatIds: chatIds), cancellationToken);
    }
}
