using web.Repositories.Events.Dtos;

namespace web.Managers.Dashboard.Dtos;

public class DashboardEventDto
{
    public EventDto Event { get; set; } = new();
    public int ShiftCount { get; set; }
    public int DraftCount { get; set; }
    public int OpenCount { get; set; }
    public int Capacity { get; set; }
    public int Taken { get; set; }
}
