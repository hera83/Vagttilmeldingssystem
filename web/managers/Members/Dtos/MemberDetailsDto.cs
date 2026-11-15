using web.Repositories.Profiles.Dtos;
using web.Repositories.Users.Dtos;

namespace web.Managers.Members.Dtos;

/// <summary>En bruger med hele profilen, eller null hvis den ikke er udfyldt.</summary>
public class MemberDetailsDto
{
    public UserDto User { get; set; } = new();
    public ProfileDto? Profile { get; set; }
}
