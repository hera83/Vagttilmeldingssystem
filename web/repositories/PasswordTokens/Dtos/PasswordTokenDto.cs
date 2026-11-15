using web.Data.Models;

namespace web.Repositories.PasswordTokens.Dtos;

public class PasswordTokenDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public PasswordTokenPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
