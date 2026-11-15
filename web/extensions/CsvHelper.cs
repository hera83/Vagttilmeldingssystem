using System.Text;

namespace web.Extensions;

/// <summary>CSV til dansk Excel: semikolon som skilletegn, UTF-8 med BOM (så æ, ø og å vises rigtigt) og CRLF mellem linjer.</summary>
public static class CsvHelper
{
    public const char Separator = ';';
    public const string NewLine = "\r\n";

    /// <summary>Én linje af felter, hvert felt escapet.</summary>
    public static string Line(IEnumerable<string?> values) => string.Join(Separator, values.Select(Escape));

    /// <summary>
    /// Escaper ét felt. Felter, der starter med = + - @ (eller tab/CR), får et ' foran, så Excel ikke tolker dem som en
    /// formel (CSV-injection fra tekst, brugerne selv har skrevet). Felter med ; " eller linjeskift sættes i anførselstegn.
    /// </summary>
    public static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Length > 0 && "=+-@\t\r".Contains(text[0]))
        {
            text = "'" + text;
        }

        return text.IndexOfAny([Separator, '"', '\n', '\r']) >= 0
            ? "\"" + text.Replace("\"", "\"\"") + "\""
            : text;
    }

    public static byte[] ToUtf8WithBom(string csv) => [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv)];
}
