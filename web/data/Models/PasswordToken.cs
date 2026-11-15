namespace web.Data.Models;

/// <summary>Engangslink til at vælge adgangskode. Kun SHA-256-hashet af tokenet gemmes.</summary>
public class PasswordToken : EntityBase
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public PasswordTokenPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
