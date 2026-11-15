namespace web.Infrastructure.Notifications;

/// <summary>Én vagt i en bekræftelsesmail.</summary>
public record MailShiftLine(string Title, string EventName, string When, string Location);
