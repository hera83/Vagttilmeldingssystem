using web.Repositories.Assignments.Dtos;
using web.Repositories.Notifications.Dtos;
using web.Repositories.Profiles.Dtos;
using web.Repositories.Users.Dtos;

namespace web.Managers.Members.Dtos;

/// <summary>Alt, systemet har om én bruger (GDPR art. 15). Aldrig adgangskode-hash, security stamp eller push-nøgler.</summary>
public class MemberDataExportDto
{
    public DateTime ExportedAtUtc { get; set; }
    public UserDto User { get; set; } = new();
    public ProfileDto? Profile { get; set; }
    public bool EmailNotifications { get; set; }
    public List<AssignmentHistoryDto> Assignments { get; set; } = [];
    public List<MemberDeviceDto> Devices { get; set; } = [];
    public List<NotificationDto> Notifications { get; set; } = [];
}
