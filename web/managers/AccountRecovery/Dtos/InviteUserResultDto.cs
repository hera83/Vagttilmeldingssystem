using web.Repositories.Users.Dtos;

namespace web.Managers.AccountRecovery.Dtos;

public class InviteUserResultDto
{
    public InviteUserStatus Status { get; set; }
    public UserDto? User { get; set; }
}
