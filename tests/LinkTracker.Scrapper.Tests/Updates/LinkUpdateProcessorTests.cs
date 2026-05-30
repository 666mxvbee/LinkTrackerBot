using LinkTracker.Scrapper.Configuration;
using LinkTracker.Scrapper.Repositories;
using LinkTracker.Scrapper.Services.Notifications;
using LinkTracker.Scrapper.Services.Updates;
using LinkTracker.Shared.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Tests.Updates;

public class LinkUpdateProcessorTests
{
    [Fact]
    public async Task ProcessAsync_UsesConfiguredBatchAndProcessesLinksInParallel()
    {
        var links = new[]
        {
            Link("https://github.com/octo/one"),
            Link("https://github.com/octo/two"),
            Link("https://github.com/octo/three")
        };
        var repository = new FakeLinkRepository(links);
        var checker = new FakeChecker(delay: TimeSpan.FromMilliseconds(50));
        var sender = new CapturingMessageSender();
        var processor = CreateProcessor(repository, checker, sender, batchSize: 10, parallelism: 2);

        await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal((0, 50), repository.BatchRequests.Single());
        Assert.Equal(3, sender.Sent.Count);
        Assert.Equal(3, repository.UpdatedUrls.Count);
        Assert.InRange(checker.MaxConcurrentCalls, 2, 2);
    }

    [Fact]
    public async Task ProcessAsync_IsolatesLinkFailuresAndSendsFailureReport()
    {
        var links = new[]
        {
            Link("https://github.com/octo/good"),
            Link("https://github.com/octo/bad")
        };
        var repository = new FakeLinkRepository(links);
        var checker = new FakeChecker(url => url.Contains("/bad", StringComparison.OrdinalIgnoreCase));
        var sender = new CapturingMessageSender();
        var processor = CreateProcessor(repository, checker, sender, batchSize: 50, parallelism: 2);

        await processor.ProcessAsync(CancellationToken.None);

        Assert.Contains(sender.Sent, update => update.Url.EndsWith("/good") && update.Description.Contains("Title:"));
        Assert.Contains(sender.Sent, update => update.Url.EndsWith("/bad") && update.Description.Contains("Failed to check link."));
        Assert.Contains("https://github.com/octo/good", repository.UpdatedUrls);
        Assert.DoesNotContain("https://github.com/octo/bad", repository.UpdatedUrls);
    }

    private static LinkUpdateProcessor CreateProcessor(
        FakeLinkRepository repository,
        FakeChecker checker,
        CapturingMessageSender sender,
        int batchSize,
        int parallelism)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILinkRepository>(repository);
        services.AddSingleton<ILinkUpdateChecker>(checker);
        services.AddSingleton<IMessageSender>(sender);

        var provider = services.BuildServiceProvider();

        return new LinkUpdateProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ScrapperOptions
            {
                BatchSize = batchSize,
                Parallelism = parallelism
            }),
            NullLogger<LinkUpdateProcessor>.Instance);
    }

    private static (string Url, long[] ChatIds, DateTimeOffset LastUpdate) Link(string url)
    {
        return (url, [1001], DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    }

    private sealed class FakeLinkRepository(
        IReadOnlyList<(string Url, long[] ChatIds, DateTimeOffset LastUpdate)> links) : ILinkRepository
    {
        public List<(int Offset, int Limit)> BatchRequests { get; } = [];

        public ConcurrentBag<string> UpdatedUrls { get; } = [];

        public void AddChat(long chatId) => throw new NotSupportedException();

        public void RemoveChat(long chatId) => throw new NotSupportedException();

        public bool ChatExists(long chatId) => throw new NotSupportedException();

        public LinkResponse? AddLink(long chatId, string url, string[]? tags) => throw new NotSupportedException();

        public bool RemoveLink(long chatId, string url) => throw new NotSupportedException();

        public IEnumerable<LinkResponse> GetLinks(long chatId, string? tag = null, int offset = 0, int limit = 100) =>
            throw new NotSupportedException();

        public IEnumerable<(string Url, long[] ChatIds, DateTimeOffset LastUpdate)> GetLinksForUpdate(
            int offset = 0,
            int limit = 100)
        {
            BatchRequests.Add((offset, limit));

            return links
                .Skip(offset)
                .Take(limit)
                .ToArray();
        }

        public void UpdateLastCheckTime(string url, DateTimeOffset lastUpdate)
        {
            UpdatedUrls.Add(url);
        }
    }

    private sealed class FakeChecker : ILinkUpdateChecker
    {
        private readonly Func<string, bool> _shouldFail;
        private readonly TimeSpan _delay;
        private int _currentCalls;

        public FakeChecker(TimeSpan delay)
            : this(_ => false, delay)
        {
        }

        public FakeChecker(Func<string, bool> shouldFail)
            : this(shouldFail, TimeSpan.Zero)
        {
        }

        private FakeChecker(Func<string, bool> shouldFail, TimeSpan delay)
        {
            _shouldFail = shouldFail;
            _delay = delay;
        }

        public int MaxConcurrentCalls { get; private set; }

        public bool CanHandle(string url) => true;

        public async Task<IReadOnlyList<DetectedLinkUpdate>> CheckUpdatesAsync(
            string url,
            DateTimeOffset since,
            CancellationToken cancellationToken)
        {
            var current = Interlocked.Increment(ref _currentCalls);
            MaxConcurrentCalls = Math.Max(MaxConcurrentCalls, current);

            try
            {
                if (_delay > TimeSpan.Zero)
                {
                    await Task.Delay(_delay, cancellationToken);
                }

                if (_shouldFail(url))
                {
                    throw new HttpRequestException("External API unavailable");
                }

                return
                [
                    new DetectedLinkUpdate(
                        Url: url,
                        Kind: "Issue",
                        Title: "Updated link",
                        UserName: "user",
                        CreatedAt: DateTimeOffset.UtcNow,
                        Preview: "preview")
                ];
            }
            finally
            {
                Interlocked.Decrement(ref _currentCalls);
            }
        }
    }

    private sealed class CapturingMessageSender : IMessageSender
    {
        public ConcurrentBag<LinkUpdate> Sent { get; } = [];

        public Task SendAsync(LinkUpdate update, CancellationToken cancellationToken)
        {
            Sent.Add(update);
            return Task.CompletedTask;
        }
    }
}
