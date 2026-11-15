namespace web.Repositories.Shifts.Dtos;

/// <summary>Tal til dashboardet: kommende vagter (ikke startet) i ikke-arkiverede begivenheder.</summary>
public class ShiftOverviewDto
{
    public int OpenShiftCount { get; set; }
    public int OpenShiftsWithFreeSeats { get; set; }
    public int OpenFreeSeats { get; set; }
    public int DraftShiftCount { get; set; }

    /// <summary>Pladser i alle kommende vagter (kladder og åbne).</summary>
    public int UpcomingCapacity { get; set; }

    /// <summary>Besatte pladser (aktive tildelinger) i alle kommende vagter.</summary>
    public int UpcomingTaken { get; set; }
}
