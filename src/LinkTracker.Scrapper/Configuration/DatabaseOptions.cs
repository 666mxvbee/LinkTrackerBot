namespace LinkTracker.Scrapper.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string AccessType { get; init; } = "SQL";

    public string ConnectionString { get; init; } = string.Empty;

    public bool RunMigrations { get; init; } = true;
}