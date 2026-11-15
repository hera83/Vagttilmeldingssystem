using web.Managers.Members.Dtos;

namespace web.Managers.Members.Interfaces;

public interface IMemberManager
{
    Task<List<MemberDto>> GetMembersAsync(CancellationToken cancellationToken = default);

    Task<MemberDetailsDto?> GetMemberAsync(Guid userId, CancellationToken cancellationToken = default);
}
