namespace web.Repositories.Shifts.Dtos;

public class ShiftChangeResultDto
{
    public ShiftChangeStatus Status { get; set; }

    /// <summary>Antal aktive tildelinger på vagten, da ændringen blev forsøgt.</summary>
    public int Taken { get; set; }
}
