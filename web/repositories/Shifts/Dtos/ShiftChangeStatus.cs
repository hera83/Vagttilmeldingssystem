namespace web.Repositories.Shifts.Dtos;

public enum ShiftChangeStatus
{
    Success,
    NotFound,

    /// <summary>Kapaciteten ville blive lavere end antallet af aktive tildelinger.</summary>
    CapacityBelowTaken,

    /// <summary>Vagten har aktive tildelinger og kan ikke slettes.</summary>
    HasActiveAssignments
}
