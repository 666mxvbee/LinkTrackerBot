using DbUp;
using LinkTracker.Scrapper.Configuration;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Database;

public static class MigrationRunner
{
    public static void Run(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<DatabaseOptions>>()
            .Value;

        if (!options.RunMigrations)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }

        var migrationsPath = Path.Combine(AppContext.BaseDirectory, "migrations");

        if (!Directory.Exists(migrationsPath))
        {
            throw new DirectoryNotFoundException($"Migrations directory not found: {migrationsPath}");
        }

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(options.ConnectionString)
            .WithScriptsFromFileSystem(migrationsPath)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw new Exception($"Failed to upgrade migrations: {result.Error}");
        }
    }
}