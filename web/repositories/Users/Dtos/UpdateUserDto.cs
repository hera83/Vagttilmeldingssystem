namespace web.Repositories.Users.Dtos;

public class UpdateUserDto
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
}
