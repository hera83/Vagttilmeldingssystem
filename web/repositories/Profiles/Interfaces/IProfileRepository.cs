using web.Repositories.Profiles.Dtos;

namespace web.Repositories.Profiles.Interfaces;

public interface IProfileRepository
{
    Task<ProfileDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsCompleteAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<HashSet<Guid>> GetCompleteUserIdsAsync(CancellationToken cancellationToken = default);

    Task<ProfileDto> SaveAsync(Guid userId, SaveProfileDto dto, CancellationToken cancellationToken = default);
}
