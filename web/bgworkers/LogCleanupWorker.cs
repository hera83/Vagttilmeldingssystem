using Microsoft.Data.Sqlite;
using web.Infrastructure.Logging;

namespace web.BgWorkers;

// Rent vedligeholdelsesjob med fast interval. Det bruger kun singletons og skal derfor ikke oprette et scope.
public class LogCleanupWorker(LogDatabaseSettings settings, ILogger<LogCleanupWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private const int MinRowsPerRound = 1000;
    private const int MaxRounds = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            do
            {
                try
                {
                    await CleanupAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Forventet ved nedlukning.
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Worker} failed", nameof(LogCleanupWorker));
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // WaitForNextTickAsync kaster, når stoppingToken annulleres.
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.DatabasePath))
        {
            return;
        }

        await using var connection = new SqliteConnection(settings.ReadWriteConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Vent op til 5 sekunder, hvis sinken skriver lige nu.
        await ExecuteAsync(connection, "PRAGMA busy_timeout = 5000", cancellationToken);

        // log.db kører i WAL-tilstand: nye rækker (og resultatet af VACUUM) ligger i log.db-wal,
        // indtil der laves checkpoint. Uden checkpoint er filstørrelsen ikke retvisende.
        await ExecuteAsync(connection, "PRAGMA wal_checkpoint(TRUNCATE)", cancellationToken);
        var file = new FileInfo(settings.DatabasePath);
        if (file.Length <= LogDatabaseSettings.MaxSizeBytes)
        {
            return;
        }

        var sizeBefore = file.Length;
        var deletedTotal = 0;
        for (var round = 0; round < MaxRounds && file.Length > LogDatabaseSettings.MaxSizeBytes; round++)
        {
            await using (var delete = connection.CreateCommand())
            {
                delete.CommandText = $"""
                    DELETE FROM {LogDatabaseSettings.TableName}
                    WHERE id IN (
                        SELECT id FROM {LogDatabaseSettings.TableName}
                        ORDER BY id
                        LIMIT MAX(@min, (SELECT COUNT(*) FROM {LogDatabaseSettings.TableName}) / 10))
                    """;
                delete.Parameters.AddWithValue("@min", MinRowsPerRound);
                var deleted = await delete.ExecuteNonQueryAsync(cancellationToken);
                deletedTotal += deleted;
                if (deleted == 0)
                {
                    break;
                }
            }

            await ExecuteAsync(connection, "VACUUM", cancellationToken);
            await ExecuteAsync(connection, "PRAGMA wal_checkpoint(TRUNCATE)", cancellationToken);
            file.Refresh();
        }

        logger.LogWarning("log.db was over {MaxSizeBytes} bytes: deleted {DeletedRows} rows, size {SizeBefore} -> {SizeAfter} bytes",
            LogDatabaseSettings.MaxSizeBytes, deletedTotal, sizeBefore, file.Length);
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
