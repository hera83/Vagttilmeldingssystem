namespace web.Repositories.PasswordTokens.Dtos;

/// <summary>Det rå token til linket. Det gemmes aldrig og findes kun i dette svar.</summary>
public class IssuedPasswordTokenDto
{
    public PasswordTokenDto PasswordToken { get; set; } = new();
    public string Token { get; set; } = string.Empty;
}
