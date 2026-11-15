namespace web.Managers.Planning.Dtos;

public enum PlanningResult
{
    Success,
    EventNotFound,
    ShiftNotFound,

    /// <summary>Vagtens startdato ligger uden for begivenhedens periode.</summary>
    OutsideEventPeriod,

    /// <summary>Begivenheden har vagter og kan derfor ikke slettes (kun arkiveres).</summary>
    EventHasShifts,

    /// <summary>Det nye antal pladser er lavere end antallet af personer, der allerede er på vagten.</summary>
    CapacityBelowTaken,

    /// <summary>Vagten har aktive tildelinger og kan ikke slettes. Fjern personerne først.</summary>
    ShiftHasAssignments,

    /// <summary>Begivenheden er arkiveret, så der kan ikke åbnes for tilmelding.</summary>
    EventArchived
}
