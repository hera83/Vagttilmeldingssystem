namespace web.Repositories.Assignments.Dtos;

public enum AssignmentActivityKind
{
    /// <summary>Brugeren tilmeldte sig selv.</summary>
    Enrolled,

    /// <summary>En administrator satte brugeren på.</summary>
    Assigned,

    /// <summary>En administrator fjernede brugeren.</summary>
    Removed
}
