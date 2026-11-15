using web.Data.Models;

namespace web.Repositories.Assignments.Dtos;

/// <summary>Én bruger på én eller flere vagter. Alt eller intet.</summary>
public class CreateAssignmentsDto
{
    public Guid UserId { get; set; }
    public List<Guid> ShiftIds { get; set; } = [];
    public AssignmentSource Source { get; set; }
    public Guid? AssignedByUserId { get; set; }

    /// <summary>
    /// true: kun åbne vagter i ikke-arkiverede begivenheder, der ikke er startet (brugerens egen tilmelding).
    /// false: også kladder (administratorens forhåndstildeling).
    /// </summary>
    public bool OnlyOpen { get; set; }
}
