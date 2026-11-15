using web.Repositories.Events.Dtos;

namespace web.Managers.Planning.Dtos;

public class EventListItemDto
{
    public EventDto Event { get; set; } = new();
    public int DraftCount { get; set; }
    public int OpenCount { get; set; }
}
