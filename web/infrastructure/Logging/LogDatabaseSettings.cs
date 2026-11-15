using Microsoft.Data.Sqlite;

namespace web.Infrastructure.Logging;

public class LogDatabaseSettings(string contentRootPath)
{
    public const string TableName = "Logs";

    /// <summary>Loftet for log.db. LogCleanupWorker sletter de ældste rækker, når filen er større.</summary>
    public const long MaxSizeBytes = 1L * 1024 * 1024 * 1024;

    /// <summary>Sinkens egen grænse (PRAGMA max_page_count) i MB. Skal ligge over MaxSizeBytes, ellers holder sinken op med at skrive, før oprydningen når at køre.</summary>
    public const uint SinkMaxSizeMegabytes = 2048;

    public string DatabasePath { get; } = Path.Combine(contentRootPath, "app_dbs", "log.db");

    public string ReadOnlyConnectionString => new SqliteConnectionStringBuilder { DataSource = DatabasePath, Mode = SqliteOpenMode.ReadOnly }.ToString();

    public string ReadWriteConnectionString => new SqliteConnectionStringBuilder { DataSource = DatabasePath, Mode = SqliteOpenMode.ReadWrite }.ToString();
}
