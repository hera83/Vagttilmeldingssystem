namespace web.Managers.Planning.Dtos;

public enum PlanningResult
{
    Success,
    EventNotFound,
    ShiftNotFound,

    /// <summary>Vagtens startdato ligger uden for begivenhedens periode.</summary>
    OutsideEventPeriod,

    /// <summary>Begivenheden har vagter og kan derfor ikke slettes (kun arkiveres).</summary>
    EventHasShifts
}
