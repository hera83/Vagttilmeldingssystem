namespace web.ViewModels.Users;

/// <summary>Administratorens skrivebeskyttede visning af en brugers profil.</summary>
public class ProfileUsersViewModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool HasProfile { get; set; }
    public bool IsComplete { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ShirtSize { get; set; } = string.Empty;
    public int DrinkTickets { get; set; }
    public int TivoliSheets { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string ChildClass { get; set; } = string.Empty;
    public string UpdatedText { get; set; } = string.Empty;
}
