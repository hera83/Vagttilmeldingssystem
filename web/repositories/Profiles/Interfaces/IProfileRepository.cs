using web.Repositories.Profiles.Dtos;

namespace web.Repositories.Profiles.Interfaces;

public interface IProfileRepository
{
    Task<ProfileDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsCompleteAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<HashSet<Guid>> GetCompleteUserIdsAsync(CancellationToken cancellationToken = default);

    Task<ProfileDto> SaveAsync(Guid userId, SaveProfileDto dto, CancellationToken cancellationToken = default);

    /// <summary>Slår e-mail-beskeder til/fra. Opretter en (tom) profil, hvis brugeren ikke har en.</summary>
    Task SetEmailNotificationsAsync(Guid userId, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Er e-mail-beskeder slået til? true, hvis brugeren ikke har en profil endnu (standard).</summary>
    Task<bool> GetEmailNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Brugere, der har slået e-mail-beskeder fra.</summary>
    Task<HashSet<Guid>> GetEmailOptOutUserIdsAsync(CancellationToken cancellationToken = default);
}
