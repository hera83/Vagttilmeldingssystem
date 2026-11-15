namespace web.Managers.Members.Dtos;

public enum DeleteMemberResult
{
    Deleted,
    NotFound,

    /// <summary>Brugeren har (eller har haft) vagter. Slet ikke, deaktivér i stedet, så historikken bevares.</summary>
    HasAssignments
}
