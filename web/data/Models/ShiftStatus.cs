namespace web.Data.Models;

/// <summary>En vagts tilstand. Gemmes som tekst.</summary>
public enum ShiftStatus
{
    /// <summary>Skjult kladde: kun administratorer (og evt. forhåndstildelte brugere) kan se den.</summary>
    Draft,

    /// <summary>Åben for tilmelding: alle brugere kan se den og tilmelde sig ledige pladser.</summary>
    Open
}
