using web.Managers.Members.Dtos;

namespace web.Managers.Members.Interfaces;

public interface IMemberManager
{
    Task<List<MemberDto>> GetMembersAsync(CancellationToken cancellationToken = default);

    Task<MemberDetailsDto?> GetMemberAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Sletter brugeren (og profilen), men kun hvis brugeren aldrig har haft vagter.</summary>
    Task<DeleteMemberResult> DeleteMemberAsync(Guid userId, CancellationToken cancellationToken = default);
}
