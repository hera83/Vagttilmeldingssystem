namespace web.ViewModels.Users;

public class UserListItemViewModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public string LastLoginText { get; set; } = string.Empty;
    public bool IsCurrentUser { get; set; }
}
