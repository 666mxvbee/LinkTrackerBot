using LinkTracker.Scrapper.Database;
using LinkTracker.Scrapper.Repositories;
using LinkTracker.Scrapper.Repositories.Orm;
using LinkTracker.Scrapper.Repositories.Sql;

namespace LinkTracker.Scrapper.Tests.Postgres;

[Collection(PostgresCollection.Name)]
public class RepositoryTests(PostgresFixture fixture)
{
    [Theory]
    [InlineData("SQL")]
    [InlineData("ORM")]
    public async Task ChatCrud_Works(string accessType)
    {
        await fixture.ResetDatabaseAsync();

        using var context = CreateRepositoryContext(accessType);

        context.Links.AddChat(101);

        Assert.True(context.Links.ChatExists(101));

        context.Links.RemoveChat(101);

        Assert.False(context.Links.ChatExists(101));
    }

    [Theory]
    [InlineData("SQL")]
    [InlineData("ORM")]
    public async Task LinkSubscriptionCrud_Works(string accessType)
    {
        await fixture.ResetDatabaseAsync();

        using var context = CreateRepositoryContext(accessType);
        var url = $"https://github.com/dotnet/runtime-{Guid.NewGuid():N}";

        context.Links.AddChat(202);

        var added = context.Links.AddLink(202, url, ["dotnet", "github"]);
        var duplicate = context.Links.AddLink(202, url, ["dotnet"]);
        var allLinks = context.Links.GetLinks(202).ToArray();
        var dotnetLinks = context.Links.GetLinks(202, "dotnet").ToArray();
        var missingTagLinks = context.Links.GetLinks(202, "missing").ToArray();
        var updateBatch = context.Links.GetLinksForUpdate().ToArray();
        var removed = context.Links.RemoveLink(202, url);

        Assert.NotNull(added);
        Assert.Null(duplicate);
        Assert.Single(allLinks);
        Assert.Single(dotnetLinks);
        Assert.Empty(missingTagLinks);
        Assert.Contains(updateBatch, item => item.Url == url && item.ChatIds.Contains(202));
        Assert.True(removed);
        Assert.Empty(context.Links.GetLinks(202));
    }

    [Theory]
    [InlineData("SQL")]
    [InlineData("ORM")]
    public async Task TagCrud_Works(string accessType)
    {
        await fixture.ResetDatabaseAsync();

        using var context = CreateRepositoryContext(accessType);

        var created = context.Tags.Create("backend");
        var duplicate = context.Tags.Create("backend");
        var all = context.Tags.GetAll().ToArray();
        var updated = context.Tags.Update(created.Id, "backend-updated");
        var deleted = context.Tags.Delete(created.Id);

        Assert.Equal(created.Id, duplicate.Id);
        Assert.Single(all);
        Assert.Equal("backend-updated", updated?.Name);
        Assert.True(deleted);
        Assert.Null(context.Tags.Get(created.Id));
    }

    private RepositoryContext CreateRepositoryContext(string accessType)
    {
        if (accessType.Equals("ORM", StringComparison.OrdinalIgnoreCase))
        {
            var dbContext = fixture.CreateDbContext();

            return new RepositoryContext(
                new OrmLinkRepository(dbContext),
                new OrmTagRepository(dbContext),
                dbContext);
        }

        return new RepositoryContext(
            new SqlLinkRepository(fixture.DataSource),
            new SqlTagRepository(fixture.DataSource));
    }

    private sealed class RepositoryContext(
        ILinkRepository links,
        ITagRepository tags,
        LinkTrackerDbContext? dbContext = null) : IDisposable
    {
        public ILinkRepository Links { get; } = links;

        public ITagRepository Tags { get; } = tags;

        public void Dispose()
        {
            dbContext?.Dispose();
        }
    }
}
