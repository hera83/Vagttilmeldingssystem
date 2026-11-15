using System.Collections.Concurrent;
using web.Services.Push.Dtos.Messages;
using web.Services.Push.Interfaces;

namespace web.Tests.Infrastructure;

/// <summary>Falsk push-service: sender intet over nettet, men husker hvert forsøg. Opførslen kan styres pr. abonnementsadresse.</summary>
public class FakePushService : IPushService
{
    public const string PublicKey = "BFakePublicKeyForTestsOnly";

    public bool Enabled { get; set; } = true;

    /// <summary>Alle beskeder, der er "leveret".</summary>
    public ConcurrentQueue<SendPushRequestDto> Delivered { get; } = new();

    /// <summary>Opførsel pr. adresse: returnér et svar eller kast en PushException. Uden opførsel leveres beskeden.</summary>
    public ConcurrentDictionary<string, Func<SendPushRequestDto, SendPushResponseDto>> Behaviors { get; } = new();

    public Task<string?> GetPublicKeyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Enabled ? PublicKey : null);

    public Task<SendPushResponseDto> SendAsync(SendPushRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = Behaviors.TryGetValue(request.Endpoint, out var behavior)
            ? behavior(request)
            : new SendPushResponseDto { Status = PushDeliveryStatus.Delivered };
        if (response.Status == PushDeliveryStatus.Delivered)
        {
            Delivered.Enqueue(request);
        }

        return Task.FromResult(response);
    }

    public List<SendPushRequestDto> DeliveredTo(string endpoint) => Delivered.Where(x => x.Endpoint == endpoint).ToList();

    public static string NewEndpoint() => $"https://fcm.googleapis.com/fcm/send/test-{Guid.NewGuid():N}";
}
