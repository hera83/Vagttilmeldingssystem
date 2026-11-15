namespace web.Managers.Reminders.Dtos;

public class ReminderScanResultDto
{
    /// <summary>Tidspunktet, scanningen regnede ud fra (UTC).</summary>
    public DateTime ScannedAtUtc { get; set; }

    /// <summary>Tildelinger, der var en påmindelse til (48 t eller 24 t).</summary>
    public int DueCount { get; set; }

    /// <summary>Nye beskeder i køen (push og e-mail). Beskeder, der allerede lå i køen, tæller ikke.</summary>
    public int QueuedCount { get; set; }
}
