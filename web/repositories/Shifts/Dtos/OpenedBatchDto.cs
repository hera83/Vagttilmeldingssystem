namespace web.Repositories.Shifts.Dtos;

/// <summary>Én frigivelse (samme begivenhed og samme OpenedAt), som der endnu ikke er sendt beskeder om.</summary>
public class OpenedBatchDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }

    /// <summary>Alle vagter i frigivelsen. De markeres samlet med MarkOpenNotifiedAsync.</summary>
    public List<Guid> ShiftIds { get; set; } = [];

    /// <summary>Hvor mange af dem, der stadig kan tilmeldes nu (åbne, ikke startet, begivenheden ikke arkiveret).</summary>
    public int AvailableCount { get; set; }
}
