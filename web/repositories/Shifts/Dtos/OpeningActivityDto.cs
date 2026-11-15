namespace web.Repositories.Shifts.Dtos;

/// <summary>Én frigivelse (samme begivenhed og samme OpenedAt) til "Seneste aktivitet".</summary>
public class OpeningActivityDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public int ShiftCount { get; set; }
    public string? OpenedByName { get; set; }
}
