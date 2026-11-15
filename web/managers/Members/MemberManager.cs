using web.Managers.Members.Dtos;
using web.Managers.Members.Interfaces;
using web.Repositories.Assignments.Interfaces;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.Users.Interfaces;

namespace web.Managers.Members;

public class MemberManager(
    IUserRepository userRepository,
    IProfileRepository profileRepository,
    IAssignmentRepository assignmentRepository) : IMemberManager
{
    public async Task<DeleteMemberResult> DeleteMemberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await assignmentRepository.AnyForUserAsync(userId, cancellationToken))
        {
            return DeleteMemberResult.HasAssignments;
        }

        return await userRepository.DeleteAsync(userId, cancellationToken) ? DeleteMemberResult.Deleted : DeleteMemberResult.NotFound;
    }

    public async Task<List<MemberDto>> GetMembersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        var complete = await profileRepository.GetCompleteUserIdsAsync(cancellationToken);
        return users.Select(user => new MemberDto { User = user, HasCompleteProfile = complete.Contains(user.Id) }).ToList();
    }

    public async Task<MemberDetailsDto?> GetMemberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return new MemberDetailsDto
        {
            User = user,
            Profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken)
        };
    }
}
