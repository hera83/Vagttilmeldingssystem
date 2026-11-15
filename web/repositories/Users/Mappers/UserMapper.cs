using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.Users.Dtos;

namespace web.Repositories.Users.Mappers;

public static class UserMapper
{
    public static readonly Expression<Func<User, UserDto>> ToDtoExpression = x => new UserDto
    {
        Id = x.Id,
        UserName = x.UserName,
        DisplayName = x.DisplayName,
        SecurityStamp = x.SecurityStamp,
        IsAdmin = x.IsAdmin,
        IsActive = x.IsActive,
        LastLoginAt = x.LastLoginAt,
        CreatedAt = x.CreatedAt
    };

    private static readonly Func<User, UserDto> CompiledToDto = ToDtoExpression.Compile();

    public static UserDto ToDto(this User entity) => CompiledToDto(entity);

    public static User ToEntity(this CreateUserDto dto) => new()
    {
        // PasswordHash sættes af repositoriet, som ejer hashingen.
        UserName = dto.UserName.Trim(),
        DisplayName = dto.DisplayName.Trim(),
        IsAdmin = dto.IsAdmin,
        IsActive = true
    };

    public static void ApplyTo(this UpdateUserDto dto, User entity)
    {
        entity.DisplayName = dto.DisplayName.Trim();
        entity.IsAdmin = dto.IsAdmin;
        if (entity.IsActive != dto.IsActive)
        {
            entity.IsActive = dto.IsActive;
            entity.SecurityStamp = Guid.NewGuid();
        }
    }
}
