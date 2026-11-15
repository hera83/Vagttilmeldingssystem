using System.Globalization;
using Microsoft.Data.Sqlite;
using web.Infrastructure.Logging;
using web.Repositories.Logs.Dtos;
using web.Repositories.Logs.Interfaces;
using web.Repositories.Logs.Mappers;

namespace web.Repositories.Logs;

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
