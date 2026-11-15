using web.Repositories.Users.Dtos;

namespace web.Repositories.Users.Interfaces;

public interface IUserRepository
{
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserDto?> ValidateCredentialsAsync(string userName, string password, CancellationToken cancellationToken = default);

    Task<UserDto?> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default);

    Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default);

    Task<bool> SetPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default);

    Task<UserDto?> ChangePasswordAsync(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
