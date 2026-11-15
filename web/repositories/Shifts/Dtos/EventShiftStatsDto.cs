namespace web.Repositories.Shifts.Dtos;

/// <summary>Vagter, pladser og besatte pladser for én begivenhed (alle vagter, også kladder).</summary>
public class EventShiftStatsDto
{
    public Guid EventId { get; set; }
    public int ShiftCount { get; set; }
    public int DraftCount { get; set; }
    public int OpenCount { get; set; }
    public int Capacity { get; set; }
    public int Taken { get; set; }
}
