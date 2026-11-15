using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.PasswordTokens.Dtos;

namespace web.Repositories.PasswordTokens.Mappers;

public static class PasswordTokenMapper
{
    public static readonly Expression<Func<PasswordToken, PasswordTokenDto>> ToDtoExpression = x => new PasswordTokenDto
    {
        Id = x.Id,
        UserId = x.UserId,
        Purpose = x.Purpose,
        ExpiresAt = x.ExpiresAt,
        UsedAt = x.UsedAt
    };

    private static readonly Func<PasswordToken, PasswordTokenDto> CompiledToDto = ToDtoExpression.Compile();

    public static PasswordTokenDto ToDto(this PasswordToken entity) => CompiledToDto(entity);

    public static PasswordToken ToEntity(this CreatePasswordTokenDto dto, string tokenHash) => new()
    {
        UserId = dto.UserId,
        Purpose = dto.Purpose,
        ExpiresAt = dto.ExpiresAt,
        TokenHash = tokenHash
    };
}
