namespace web.Repositories.Users.Dtos;

public class CreateUserDto
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Klartekst. Hashes i repositoriet og gemmes aldrig.</summary>
    public string Password { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }
}
