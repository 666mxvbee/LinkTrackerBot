using DbUp;
using LinkTracker.Scrapper.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace LinkTracker.Scrapper.Tests.Postgres;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("linktracker_tests")
        .WithUsername("linktracker")
        .WithPassword("linktracker")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        DataSource = NpgsqlDataSource.Create(ConnectionString);
        RunMigrations();
    }

    public async Task DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await _container.DisposeAsync();
    }

    public LinkTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LinkTrackerDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new LinkTrackerDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            TRUNCATE TABLE
                chat_link_tags,
                chat_links,
                tags,
                links,
                chats
            RESTART IDENTITY CASCADE;
            """, connection);

        await command.ExecuteNonQueryAsync();
    }

    private void RunMigrations()
    {
        var migrationsPath = Path.Combine(AppContext.BaseDirectory, "migrations");

        var result = DeployChanges.To
            .PostgresqlDatabase(ConnectionString)
            .WithScriptsFromFileSystem(migrationsPath)
            .LogToConsole()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
        {
            throw new InvalidOperationException("Failed to run test migrations.", result.Error);
        }
    }
}
