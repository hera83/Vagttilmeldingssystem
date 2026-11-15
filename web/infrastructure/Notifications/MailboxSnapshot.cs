namespace web.Infrastructure.Notifications;

/// <summary>Øjebliksbillede af MailboxStatus. UnreadCount er null, før postkassen er tjekket første gang.</summary>
public sealed record MailboxSnapshot
{
    public bool IsConfigured { get; init; }
    public int? UnreadCount { get; init; }
    public DateTime? CheckedAt { get; init; }
    public string? Error { get; init; }
}
