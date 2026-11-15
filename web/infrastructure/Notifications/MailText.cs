namespace web.Infrastructure.Notifications;

/// <summary>Emne og brødtekst til en mail (uden hilsen; den tilføjes ved afsendelse).</summary>
public record MailText(string Subject, string Body);
