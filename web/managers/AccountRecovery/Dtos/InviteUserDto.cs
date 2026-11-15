namespace web.Managers.AccountRecovery.Dtos;

public class InviteUserDto
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
