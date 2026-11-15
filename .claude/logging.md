# Logging (Serilog)

**Gælder for:** alle projekter: logging-opsætning, log-databasen og log-søgning.
**Kode (standardkomponenter):** opsætningen i `Program.cs` (se `program.md`), `infrastructure/Logging/LogDatabaseSettings.cs`, `repositories/Logs/` (`ILogRepository`/`LogRepository`), `bgworkers/LogCleanupWorker.cs` og i API-delen `LogsController` + `dtos/Logs/`.
**Se også:** `databases.md`, `errorhandling.md`, `bgworkers.md`.

## Sådan virker det

- Serilog erstatter standard-loggeren (`builder.Host.UseSerilog(...)`). Niveauerne sættes i koden i `Program.cs`: minimum `Information`, og `Microsoft.AspNetCore` og `Microsoft.EntityFrameworkCore` på `Warning`. Uden den sidste skriver EF Core hver eneste SQL-sætning i log.db. Ret niveauerne i `Program.cs` og ikke i `Logging:LogLevel` i appsettings.
- Der er to sinks: `Console` og SQLite-databasen `app_dbs/log.db` (pakken `Serilog.Sinks.SQLite`, tabel `Logs`, tidsstempler i UTC).
- Tabellen oprettes af sinken: `id INTEGER PRIMARY KEY AUTOINCREMENT, Timestamp TEXT, Level VARCHAR(10), Exception TEXT, RenderedMessage TEXT, Properties TEXT`. `Timestamp` er tekst i formatet `yyyy-MM-ddTHH:mm:ss.fff` (UTC), så tekst-sammenligning svarer til tids-sammenligning.
- `LogDatabaseSettings` (singleton) holder den absolutte sti til `log.db` (bygget ud fra `ContentRootPath`), tabelnavnet, loftet `MaxSizeBytes` på 1 GiB og sinkens egen grænse `SinkMaxSizeMegabytes`.
- Sinken kører med `rollOver: false`, så den laver aldrig backup-filer. I stedet tjekker `LogCleanupWorker` filstørrelsen hvert 15. minut. Først laves et WAL-checkpoint (`PRAGMA wal_checkpoint(TRUNCATE)`), for i WAL-tilstand ligger de nyeste rækker i `log.db-wal`, så `log.db` alene er mindre end de reelle data. Er filen over loftet, sletter den de ældste ~10 % af rækkerne (mindst 1000) og kører `VACUUM`, indtil filen er under loftet. Det er en størrelsesgrænse og ikke en tidsbaseret opbevaringspolitik.
- **Sinkens grænse skal ligge over loftet.** Sinken sætter `PRAGMA max_page_count` ud fra `maxDatabaseSize` (standard 10 MB). Med `rollOver: false` holder den bare op med at skrive, når grænsen er nået. Derfor er `SinkMaxSizeMegabytes` 2048, altså dobbelt så meget som loftet, så oprydningen altid når at køre først.
- `LogRepository` laver read-only, parametriserede ADO.NET-forespørgsler mod `log.db`: filter på level, delstreng i den renderede besked, UTC-tidsinterval og paging (højst 500 pr. side).
- API-delen udstiller søgningen via `LogsController` (`GET /Logs/Search`, kombineret `/api/Logs/Search`), som kræver master-nøglen (`MasterKeyOnly`). Et rent web-projekt har ingen log-søgning, medmindre projektfilen siger det.

## Regler for at logge

1. Injicér `ILogger<T>`. Brug aldrig Serilogs statiske `Log`-klasse.
2. Brug message templates med navngivne pladsholdere, aldrig string interpolation: `logger.LogWarning("Deleted {Count} rows", count)`.
3. Exceptions logges med exception'en som første argument: `logger.LogError(ex, "…")`.
4. Log ikke uhåndterede exceptions i controllere. Det gør `GlobalExceptionHandler` (se `errorhandling.md`).
5. Log aldrig secrets eller persondata: API-nøgler, adgangskoder, tokens, cookies og hele request-bodies.
6. Skriv aldrig selv i `log.db`. Kun Serilog og `LogCleanupWorker` må.

## Skabeloner

```csharp
// infrastructure/Logging/LogDatabaseSettings.cs
using Microsoft.Data.Sqlite;

namespace <projekt>.Infrastructure.Logging;

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
```

`repositories/Logs/` følger `repositories.md` uden EF Core. Dtos: `LogDto` (`Id`, `TimestampUtc`, `Level`, `Message`, `Exception?`, `Properties?`), `SearchLogsDto` (`Level?`, `Text?`, `FromUtc?`, `ToUtc?`, `Page` = 1, `PageSize` = 50) og `LogSearchResultDto` (`Items`, `TotalCount`, `Page`, `PageSize`). Metoden er `SearchAsync(SearchLogsDto search)`.

```csharp
// repositories/Logs/Mappers/LogMapper.cs
using System.Globalization;
using <projekt>.Repositories.Logs.Dtos;
using Microsoft.Data.Sqlite;

namespace <projekt>.Repositories.Logs.Mappers;

public static class LogMapper
{
    /// <summary>Kolonnerne i sinkens tabel, i den rækkefølge ToDto læser dem.</summary>
    public const string Columns = "id, Timestamp, Level, RenderedMessage, Exception, Properties";

    public static LogDto ToDto(this SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        TimestampUtc = DateTime.SpecifyKind(DateTime.Parse(reader.GetString(1), CultureInfo.InvariantCulture), DateTimeKind.Utc),
        Level = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
        Message = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
        Exception = reader.IsDBNull(4) || reader.GetString(4).Length == 0 ? null : reader.GetString(4),
        Properties = reader.IsDBNull(5) ? null : reader.GetString(5)
    };
}

// repositories/Logs/LogRepository.cs
using System.Globalization;
using <projekt>.Infrastructure.Logging;
using <projekt>.Repositories.Logs.Dtos;
using <projekt>.Repositories.Logs.Interfaces;
using <projekt>.Repositories.Logs.Mappers;
using Microsoft.Data.Sqlite;

namespace <projekt>.Repositories.Logs;

public class LogRepository(LogDatabaseSettings settings) : ILogRepository
{
    public const int MaxPageSize = 500;

    // Samme format som sinken skriver Timestamp i.
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    public async Task<LogSearchResultDto> SearchAsync(SearchLogsDto search, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, search.Page);
        var pageSize = Math.Clamp(search.PageSize, 1, MaxPageSize);
        var result = new LogSearchResultDto { Page = page, PageSize = pageSize };

        if (!File.Exists(settings.DatabasePath))
        {
            return result;
        }

        await using var connection = new SqliteConnection(settings.ReadOnlyConnectionString);
        await connection.OpenAsync(cancellationToken);

        var where = new List<string>();
        var parameters = new List<SqliteParameter>();
        if (!string.IsNullOrWhiteSpace(search.Level))
        {
            where.Add("Level = @level");
            parameters.Add(new SqliteParameter("@level", search.Level));
        }
        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            where.Add("RenderedMessage LIKE @text ESCAPE '\\'");
            parameters.Add(new SqliteParameter("@text", "%" + EscapeLike(search.Text) + "%"));
        }
        if (search.FromUtc is not null)
        {
            where.Add("Timestamp >= @from");
            parameters.Add(new SqliteParameter("@from", search.FromUtc.Value.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture)));
        }
        if (search.ToUtc is not null)
        {
            where.Add("Timestamp < @to");
            parameters.Add(new SqliteParameter("@to", search.ToUtc.Value.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture)));
        }
        var whereSql = where.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", where);

        await using (var count = connection.CreateCommand())
        {
            count.CommandText = $"SELECT COUNT(*) FROM {LogDatabaseSettings.TableName}{whereSql}";
            count.Parameters.AddRange(parameters.Select(Clone));
            result.TotalCount = Convert.ToInt32(await count.ExecuteScalarAsync(cancellationToken));
        }

        await using var query = connection.CreateCommand();
        query.CommandText = $"SELECT {LogMapper.Columns} FROM {LogDatabaseSettings.TableName}{whereSql} ORDER BY id DESC LIMIT @take OFFSET @skip";
        query.Parameters.AddRange(parameters.Select(Clone));
        query.Parameters.AddWithValue("@take", pageSize);
        query.Parameters.AddWithValue("@skip", (page - 1) * pageSize);

        await using var reader = await query.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Items.Add(reader.ToDto());
        }

        return result;
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    // En SqliteParameter kan kun høre til én command ad gangen.
    private static SqliteParameter Clone(SqliteParameter parameter) => new(parameter.ParameterName, parameter.Value);
}
```

```csharp
// bgworkers/LogCleanupWorker.cs
using <projekt>.Infrastructure.Logging;
using Microsoft.Data.Sqlite;

namespace <projekt>.BgWorkers;

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
```

## `LogsController` (API-delen)

Har `[Authorize(Policy = AuthorizationPolicies.MasterKeyOnly)]` og bygges efter API-skabelonen i `controllers.md` med DTO'er i `dtos/Logs/`:

| Action | HTTP | Request | Response |
|---|---|---|---|
| `Search` | `GET` | `[FromQuery] SearchLogsRequestDto` (`Level?`, `Text?`, `FromUtc?`, `ToUtc?`, `Page` ≥ 1, `PageSize` 1–500) | `SearchLogsResponseDto` (`Items` af `LogItemDto`, `TotalCount`, `Page`, `PageSize`) |

Under-modellen hedder `LogItemDto` og ikke `LogDto`, så den ikke kolliderer med repositoriets `LogDto` i mapperen.

## Kendte afvigelser i koden

Ingen.
