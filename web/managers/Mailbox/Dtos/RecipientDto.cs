namespace web.Managers.Mailbox.Dtos;

/// <summary>En aktiv bruger, man kan skrive til.</summary>
public class RecipientDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
