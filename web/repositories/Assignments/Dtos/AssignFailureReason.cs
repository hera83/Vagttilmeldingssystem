namespace web.Repositories.Assignments.Dtos;

public enum AssignFailureReason
{
    /// <summary>Vagten findes ikke.</summary>
    NotFound,

    /// <summary>Vagten er ikke åben (kladde) eller hører til en arkiveret begivenhed. Kun relevant, når OnlyOpen er sat.</summary>
    NotOpen,

    /// <summary>Brugeren har allerede en aktiv tildeling på vagten.</summary>
    AlreadyAssigned,

    /// <summary>Alle pladser er taget.</summary>
    Full
}
