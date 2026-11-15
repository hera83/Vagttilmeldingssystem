namespace web.Managers.Enrollment.Dtos;

public enum EnrollStatus
{
    Success,

    /// <summary>Ingen vagter valgt.</summary>
    NothingSelected,

    /// <summary>Profilen mangler oplysninger (krav K02). Ingen tildelinger oprettet.</summary>
    ProfileIncomplete,

    /// <summary>Mindst én vagt kunne ikke tildeles. Ingen tildelinger oprettet (alt eller intet).</summary>
    Conflict
}
