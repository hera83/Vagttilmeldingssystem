namespace web.Managers.Assignments.Dtos;

public enum UnassignResult
{
    Success,

    /// <summary>Tildelingen findes ikke, hører ikke til vagten eller er allerede fjernet.</summary>
    NotFound
}
