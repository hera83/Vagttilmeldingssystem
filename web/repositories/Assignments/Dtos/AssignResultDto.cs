namespace web.Repositories.Assignments.Dtos;

/// <summary>
/// Resultatet af en (samlet) tildeling. Succeeded = alle vagter blev tildelt. Ellers er ingen tildelt,
/// og Failures fortæller, hvilke vagter der fejlede og hvorfor.
/// </summary>
public class AssignResultDto
{
    public bool Succeeded => Failures.Count == 0 && CreatedShiftIds.Count > 0;
    public List<Guid> CreatedShiftIds { get; set; } = [];
    public List<AssignFailureDto> Failures { get; set; } = [];
}
