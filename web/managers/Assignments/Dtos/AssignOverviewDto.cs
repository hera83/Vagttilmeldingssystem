using web.Repositories.Assignments.Dtos;
using web.Repositories.Events.Dtos;
using web.Repositories.Shifts.Dtos;
using web.Repositories.Users.Dtos;

namespace web.Managers.Assignments.Dtos;

/// <summary>Til administratorens tildelingsside: vagten, hvem der er på, og hvem der kan tildeles.</summary>
public class AssignOverviewDto
{
    public EventDto Event { get; set; } = new();
    public ShiftDto Shift { get; set; } = new();
    public List<AssigneeDto> Assignees { get; set; } = [];

    /// <summary>Aktive brugere, der ikke allerede er på vagten (evt. filtreret på søgning). Højst MaxCandidates.</summary>
    public List<UserDto> Candidates { get; set; } = [];

    public int CandidateTotal { get; set; }
}
