using System.Globalization;
using Microsoft.Data.Sqlite;
using web.Repositories.Logs.Dtos;

namespace web.Repositories.Logs.Mappers;

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
