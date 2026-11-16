using web.Managers.Members.Dtos;

namespace web.Managers.Members.Interfaces;

public interface IMemberManager
{
    Task<List<MemberDto>> GetMembersAsync(CancellationToken cancellationToken = default);

    Task<MemberDetailsDto?> GetMemberAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Sletter brugeren (og profilen), men kun hvis brugeren aldrig har haft vagter.</summary>
    Task<DeleteMemberResult> DeleteMemberAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Anonymiserer brugeren (GDPR) i stedet for at slette: profil, push-abonnementer, ventende beskeder og links slettes,
    /// og brugeren bliver til "Slettet bruger …" uden login. Vagthistorikken bevares. Kan køres igen, hvis den stopper halvvejs.
    /// </summary>
    Task<AnonymizeMemberResult> AnonymizeMemberAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Alt om brugeren til dataudtræk (GDPR art. 15), eller null.</summary>
    Task<MemberDataExportDto?> ExportMemberAsync(Guid userId, CancellationToken cancellationToken = default);
}
