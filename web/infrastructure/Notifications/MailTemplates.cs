using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using web.Extensions;
using System.Text.RegularExpressions;

namespace web.Infrastructure.Notifications;

/// <summary>
/// Danske mailtekster. Teksterne indeholder kun modtagerens egne oplysninger og aldrig kladder (krav K10).
/// Brødteksten lægges i køen. Ved afsendelse tilføjes "Hej {navn}" og hilsen (Wrap), og HTML laves ud fra teksten (ToHtml).
/// </summary>
public static partial class MailTemplates
{
    public const string AppName = "Vagttilmelding";

    // Koder <, >, & og anførselstegn, men lader æ, ø og å stå (som WebEncoderOptions i Program.cs).
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    public static MailText Invitation(string userName, string link, int validDays) => new(
        "Du er inviteret til Vagttilmelding",
        "Du er oprettet som bruger i Vagttilmelding, hvor du kan se ledige vagter og tilmelde dig, fx til skolefesten.\n\n" +
        $"Vælg din adgangskode her. Linket gælder i {validDays} dage og kan kun bruges én gang:\n{link}\n\n" +
        $"Du logger ind med din e-mail: {userName}\n\n" +
        "Har du ikke regnet med at blive oprettet, kan du se bort fra mailen.");

    public static MailText PasswordReset(string link, int validMinutes) => new(
        "Nulstil din adgangskode til Vagttilmelding",
        "Vi har fået en anmodning om at nulstille din adgangskode.\n\n" +
        $"Vælg en ny adgangskode her. Linket gælder i {validMinutes} minutter og kan kun bruges én gang:\n{link}\n\n" +
        "Har du ikke bedt om det, kan du se bort fra mailen. Din adgangskode er ikke ændret.");

    public static MailText EnrollmentConfirmation(IReadOnlyList<MailShiftLine> shifts, string? link)
    {
        var body = new StringBuilder(shifts.Count == 1 ? "Du er tilmeldt denne vagt:\n" : $"Du er tilmeldt disse {shifts.Count} vagter:\n");
        foreach (var shift in shifts)
        {
            body.Append($"\n• {shift.Title} – {shift.EventName}\n  {shift.When}\n  {shift.Location}\n");
        }

        body.Append("\nVagterne er låst. Kan du alligevel ikke, så kontakt administratoren.");
        if (link is not null)
        {
            body.Append($"\n\nSe dine vagter:\n{link}");
        }

        return new MailText(
            shifts.Count == 1 ? "Bekræftelse: du er tilmeldt en vagt" : $"Bekræftelse: du er tilmeldt {shifts.Count} vagter",
            body.ToString());
    }

    public static MailText ShiftsOpened(string eventName, int count, string? link)
    {
        var body = $"Der er åbnet for tilmelding til {count} {(count == 1 ? "vagt" : "vagter")} til {eventName}. Pladserne fordeles efter først-til-mølle.";
        if (link is not null)
        {
            body += $"\n\nSe de ledige vagter:\n{link}";
        }

        return new MailText($"Nye vagter er åbne: {eventName}", body);
    }

    /// <summary>"i dag", "i morgen" eller "om N dage" ud fra datoerne i dansk tid (også hen over skift til/fra sommertid).</summary>
    public static string DayPhrase(DateTime nowUtc, DateTime startUtc)
    {
        var days = DateOnly.FromDateTime(startUtc.ToDanishTime()).DayNumber - DateOnly.FromDateTime(nowUtc.ToDanishTime()).DayNumber;
        return days <= 0 ? "i dag" : days == 1 ? "i morgen" : $"om {days} dage";
    }

    /// <summary>Første linje i en påmindelse, som i projektbeskrivelsen: "Du har en vagt om 2 dage: [dato] kl. [tid]" og "Husk din vagt i morgen kl. [tid]".</summary>
    public static string ReminderLead(string kind, DateTime startUtc, DateTime nowUtc)
    {
        var phrase = DayPhrase(nowUtc, startUtc);
        return kind == NotificationKinds.Reminder48
            ? $"Du har en vagt {phrase}: {startUtc.ToDanishDateText()} kl. {startUtc.ToDanishTimeText()}."
            : $"Husk din vagt {phrase} kl. {startUtc.ToDanishTimeText()}.";
    }

    public static MailText Reminder(string kind, MailShiftLine shift, DateTime startUtc, DateTime nowUtc, string? link)
    {
        var phrase = DayPhrase(nowUtc, startUtc);
        var body = $"{ReminderLead(kind, startUtc, nowUtc)}\n\n• {shift.Title} – {shift.EventName}\n  {shift.When}\n  {shift.Location}\n\n" +
                   "Vagterne er låst. Kan du alligevel ikke, så kontakt administratoren.";
        if (link is not null)
        {
            body += $"\n\nSe vagten:\n{link}";
        }

        return new MailText(
            kind == NotificationKinds.Reminder48 ? $"Påmindelse: du har en vagt {phrase}" : $"Påmindelse: husk din vagt {phrase}",
            body);
    }

    /// <summary>Kort tekst til push: første linje + vagten. Kun brugerens egen vagt.</summary>
    public static MailText ReminderPush(string kind, MailShiftLine shift, DateTime startUtc, DateTime nowUtc) => new(
        $"Påmindelse: {shift.Title}",
        $"{ReminderLead(kind, startUtc, nowUtc)} {shift.EventName}, {shift.Location}.");

    /// <summary>Hilsen og afslutning omkring brødteksten.</summary>
    public static string Wrap(string displayName, string body) =>
        $"Hej {displayName}\n\n{body}\n\nVenlig hilsen\n{AppName}\n\nDu kan svare på denne mail, hvis du har spørgsmål.";

    /// <summary>Simpel HTML-udgave af teksten med en knap til linket. Al tekst HTML-kodes.</summary>
    public static string ToHtml(string subject, string text, string? url, string buttonText)
    {
        var paragraphs = text.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(p => "<p style=\"margin:0 0 14px;line-height:1.5;\">" +
                         string.Join("<br>", p.Split('\n').Select(line => Linkify(Encoder.Encode(line)))) + "</p>");
        var button = string.IsNullOrWhiteSpace(url)
            ? string.Empty
            : $"<p style=\"margin:20px 0;\"><a href=\"{Encoder.Encode(url)}\" style=\"display:inline-block;background:#1764B8;color:#ffffff;text-decoration:none;font-weight:700;padding:10px 18px;border-radius:6px;\">{Encoder.Encode(buttonText)}</a></p>";

        return "<!DOCTYPE html><html lang=\"da\"><head><meta charset=\"utf-8\"><title>" + Encoder.Encode(subject) + "</title></head>" +
               "<body style=\"margin:0;padding:24px;background:#F6F9FD;font-family:'Segoe UI',Arial,sans-serif;color:#13243D;\">" +
               "<div style=\"max-width:560px;margin:0 auto;background:#ffffff;border:1px solid #DFE6EF;border-radius:10px;padding:24px;\">" +
               $"<p style=\"margin:0 0 18px;font-weight:800;font-size:18px;color:#1764B8;\">{AppName}</p>" +
               string.Concat(paragraphs) + button +
               "</div></body></html>";
    }

    private static string Linkify(string encodedLine) =>
        UrlRegex().Replace(encodedLine, match => $"<a href=\"{match.Value}\" style=\"color:#1764B8;\">{match.Value}</a>");

    [GeneratedRegex(@"https?://[^\s<]+")]
    private static partial Regex UrlRegex();
}
