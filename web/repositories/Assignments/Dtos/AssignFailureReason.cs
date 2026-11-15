namespace web.Repositories.Assignments.Dtos;

public enum AssignFailureReason
{
    /// <summary>Vagten findes ikke.</summary>
    NotFound,

    /// <summary>Vagten er ikke åben (kladde), hører til en arkiveret begivenhed eller er startet. Kun relevant, når OnlyOpen er sat.</summary>
    NotOpen,

    /// <summary>Brugeren har allerede en aktiv tildeling på vagten.</summary>
    AlreadyAssigned,

    /// <summary>Alle pladser er taget.</summary>
    Full
}
