using web.Services.Push.Dtos.Messages;

namespace web.Services.Push.Interfaces;

public interface IPushService
{
    /// <summary>Den offentlige VAPID-nøgle til browseren, eller null, hvis push ikke er sat op.</summary>
    Task<string?> GetPublicKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>Sender én besked til ét abonnement (én enhed).</summary>
    Task<SendPushResponseDto> SendAsync(SendPushRequestDto request, CancellationToken cancellationToken = default);
}
