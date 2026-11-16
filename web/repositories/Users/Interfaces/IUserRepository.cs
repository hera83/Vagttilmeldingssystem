using web.Repositories.Users.Dtos;

namespace web.Repositories.Users.Interfaces;

public interface IUserRepository
{
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Slår brugeren op på brugernavnet (e-mailen) uden forskel på store og små bogstaver, eller null.</summary>
    Task<UserDto?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    Task<UserDto?> ValidateCredentialsAsync(string userName, string password, CancellationToken cancellationToken = default);

    Task<UserDto?> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default);

    Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default);

    /// <summary>Ændrer brugernavnet (e-mailen). Skifter SecurityStamp, så brugeren skal logge ind med den nye e-mail.</summary>
    Task<ChangeUserNameResult> ChangeUserNameAsync(Guid id, string newUserName, CancellationToken cancellationToken = default);

    Task<bool> SetPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default);

    Task<UserDto?> ChangePasswordAsync(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Anonymiserer brugeren (GDPR): brugernavn og navn erstattes, adgangskoden bliver ukendt, brugeren deaktiveres og mister
    /// admin-rettigheden. Rækken bevares, så vagthistorikken peger på "Slettet bruger". false, hvis brugeren ikke findes.
    /// </summary>
    Task<bool> AnonymizeAsync(Guid id, CancellationToken cancellationToken = default);
}
