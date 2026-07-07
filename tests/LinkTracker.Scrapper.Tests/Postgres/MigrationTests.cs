using Npgsql;

namespace LinkTracker.Scrapper.Tests.Postgres;

[Collection(PostgresCollection.Name)]
public class MigrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Migrations_CreateExpectedTables()
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
            ORDER BY table_name;
            """, connection);

        await using var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();

        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Contains("chats", tables);
        Assert.Contains("links", tables);
        Assert.Contains("chat_links", tables);
        Assert.Contains("tags", tables);
        Assert.Contains("chat_link_tags", tables);
        Assert.Contains("notification_outbox", tables);
        Assert.Contains("schemaversions", tables);
    }
}
