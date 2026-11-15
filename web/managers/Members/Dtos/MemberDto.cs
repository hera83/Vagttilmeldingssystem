using web.Repositories.Users.Dtos;

namespace web.Managers.Members.Dtos;

/// <summary>En bruger med status for profilen (til lister).</summary>
public class MemberDto
{
    public UserDto User { get; set; } = new();
    public bool HasCompleteProfile { get; set; }
}
