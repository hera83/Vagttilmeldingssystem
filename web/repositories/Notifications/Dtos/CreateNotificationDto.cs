using web.Data.Models;

namespace web.Repositories.Notifications.Dtos;

public class CreateNotificationDto
{
    public Guid UserId { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string DedupKey { get; set; } = string.Empty;

    /// <summary>Tidligste afsendelse (UTC).</summary>
    public DateTime NotBeforeUtc { get; set; }
}
