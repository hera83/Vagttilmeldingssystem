namespace web.Infrastructure.Realtime;

/// <summary>En ændring, der skal sendes til åbne sider efter commit.</summary>
public class ShiftUpdate
{
    public Guid EventId { get; set; }

    /// <summary>De berørte vagter.</summary>
    public IReadOnlyCollection<Guid> ShiftIds { get; set; } = [];

    /// <summary>
    /// true: vagterne er åbne, så alle indloggede må få signalet. false: kladder, så kun administratorer
    /// (og de berørte brugere personligt) får besked. Kladdernes id'er sendes aldrig til andre (krav K10).
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>true, når vagterne netop er blevet åbnet for tilmelding (frigivelsen).</summary>
    public bool Opened { get; set; }

    /// <summary>Brugere, hvis egne vagter er ændret (tildelt, fjernet, tilmeldt).</summary>
    public IReadOnlyCollection<Guid> AffectedUserIds { get; set; } = [];
}
