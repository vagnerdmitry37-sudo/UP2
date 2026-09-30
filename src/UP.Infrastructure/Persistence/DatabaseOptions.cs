namespace UP.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Options:Database";

    public string ConnectionString { get; init; } = string.Empty;

    public int MaxRetryCount { get; init; } = 3;

    public int CommandTimeoutSeconds { get; init; } = 30;

    public bool EnableSensitiveDataLogging { get; init; }
}
