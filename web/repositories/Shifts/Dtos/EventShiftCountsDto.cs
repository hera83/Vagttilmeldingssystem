namespace web.Repositories.Shifts.Dtos;

public class EventShiftCountsDto
{
    public Guid EventId { get; set; }
    public int DraftCount { get; set; }
    public int OpenCount { get; set; }
}
