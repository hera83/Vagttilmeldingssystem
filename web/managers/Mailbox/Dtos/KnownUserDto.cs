namespace web.Managers.Mailbox.Dtos;

/// <summary>En bruger, hvis e-mail står i en mail (afsender i Indbakke, modtager i Sendt).</summary>
public class KnownUserDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
