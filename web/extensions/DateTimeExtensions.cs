using System.Globalization;

namespace web.Extensions;

/// <summary>
/// Omregning mellem UTC (som alt gemmes i) og dansk tid (Europe/Copenhagen), som brugerne ser og indtaster.
/// Omregningen er eksplicit, så visningen ikke afhænger af serverens tidszone.
/// </summary>
public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo DanishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    /// <summary>da-DK med ugedagsforkortelser som i mockups ("lør."). .NET/ICU giver ellers "lør" uden punktum.</summary>
    public static readonly CultureInfo Danish = CreateDanishCulture();

    private static CultureInfo CreateDanishCulture()
    {
        var culture = new CultureInfo("da-DK");
        culture.DateTimeFormat.AbbreviatedDayNames = ["søn.", "man.", "tir.", "ons.", "tor.", "fre.", "lør."];
        return CultureInfo.ReadOnly(culture);
    }

    /// <summary>UTC → dansk tid.</summary>
    public static DateTime ToDanishTime(this DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), DanishTimeZone);

    /// <summary>Dansk dato + klokkeslæt → UTC. Returnerer null, hvis tidspunktet ikke findes (springet over ved skift til sommertid).</summary>
    public static DateTime? FromDanishTimeToUtc(this DateTime danishLocal)
    {
        var unspecified = DateTime.SpecifyKind(danishLocal, DateTimeKind.Unspecified);
        if (DanishTimeZone.IsInvalidTime(unspecified))
        {
            return null;
        }

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, DanishTimeZone);
    }

    // Kolon står i anførselstegn, fordi da-DK ellers bruger punktum som tidsseparator ("17.00"). PDF og mockups bruger "17:00".

    /// <summary>Fx "fre. 14. mar. 2025 kl. 17:00".</summary>
    public static string ToDanishDateTimeText(this DateTime utc) =>
        utc.ToDanishTime().ToString("ddd d. MMM yyyy 'kl.' HH':'mm", Danish);

    /// <summary>Fx "14.03.2025 17:00" (kort, til tabeller).</summary>
    public static string ToDanishShortDateTimeText(this DateTime utc) =>
        utc.ToDanishTime().ToString("dd'.'MM'.'yyyy HH':'mm", Danish);

    /// <summary>Fx "fre. 14. mar. 2025".</summary>
    public static string ToDanishDateText(this DateTime utc) =>
        utc.ToDanishTime().ToString("ddd d. MMM yyyy", Danish);

    /// <summary>Fx "17:00".</summary>
    public static string ToDanishTimeText(this DateTime utc) =>
        utc.ToDanishTime().ToString("HH':'mm", Danish);

    /// <summary>Fx "17:00 – 19:00", eller "22:00 – 01:00 (næste dag)", hvis vagten slutter efter midnat.</summary>
    public static string ToDanishTimeRangeText(DateTime startUtc, DateTime endUtc)
    {
        var start = startUtc.ToDanishTime();
        var end = endUtc.ToDanishTime();
        var text = $"{start.ToString("HH':'mm", Danish)} – {end.ToString("HH':'mm", Danish)}";
        return end.Date > start.Date ? $"{text} (næste dag)" : text;
    }

    /// <summary>Fx "fre. 14. mar. 2025".</summary>
    public static string ToDanishDateText(this DateOnly date) =>
        date.ToString("ddd d. MMM yyyy", Danish);

    /// <summary>Én dato, eller fx "fre. 14. mar. – lør. 15. mar. 2025", hvis perioden er flere dage.</summary>
    public static string ToDanishPeriodText(DateOnly start, DateOnly end)
    {
        if (start == end)
        {
            return start.ToDanishDateText();
        }

        var startText = start.Year == end.Year ? start.ToString("ddd d. MMM", Danish) : start.ToDanishDateText();
        return $"{startText} – {end.ToDanishDateText()}";
    }
}
