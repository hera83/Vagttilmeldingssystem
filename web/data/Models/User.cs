namespace web.Data.Models;

public class User : EntityBase
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
}
