using web.Repositories.Events.Dtos;
using web.Repositories.Shifts.Dtos;

namespace web.Managers.Planning.Dtos;

/// <summary>En begivenhed med dens vagter (evt. filtreret på status) og antal pr. status.</summary>
public class EventOverviewDto
{
    public EventDto Event { get; set; } = new();
    public List<ShiftDto> Shifts { get; set; } = [];
    public int DraftCount { get; set; }
    public int OpenCount { get; set; }
}
