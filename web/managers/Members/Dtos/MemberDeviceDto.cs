namespace web.Managers.Members.Dtos;

/// <summary>En enhed med push (uden abonnementets adresse og nøgler).</summary>
public class MemberDeviceDto
{
    public string PushService { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
}
