using web.Repositories.Events.Dtos;
using web.Repositories.Shifts.Dtos;

namespace web.Managers.Planning.Dtos;

public class ShiftEditorDto
{
    public EventDto Event { get; set; } = new();
    public ShiftDto Shift { get; set; } = new();
}
