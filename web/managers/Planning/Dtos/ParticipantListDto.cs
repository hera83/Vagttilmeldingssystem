using web.Repositories.Events.Dtos;

namespace web.Managers.Planning.Dtos;

public class ParticipantListDto
{
    public EventDto Event { get; set; } = new();
    public List<ParticipantRowDto> Rows { get; set; } = [];
}
