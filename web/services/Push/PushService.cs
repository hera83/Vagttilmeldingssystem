using System.Net;
using System.Text.Json;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using web.Services.Push.Dtos.Messages;
using web.Services.Push.Interfaces;
using WebPushSubscription = Lib.Net.Http.WebPush.PushSubscription;

namespace web.Services.Push;

/// <summary>
/// Web push (VAPID, RFC 8030/8291/8292) via Lib.Net.Http.WebPush. Light pattern med et bibliotek:
/// konfigurationen læses ved hvert kald, og alle bibliotekets fejl oversættes til PushException.
/// </summary>
public class PushService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IPushService
{
    public const string HttpClientName = "Push";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<string?> GetPublicKeyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(TryGetSettings(out var publicKey, out _, out _) ? publicKey : null);

    public async Task<SendPushResponseDto> SendAsync(SendPushRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!TryGetSettings(out var publicKey, out var privateKey, out var subject))
        {
            throw new PushException(HttpStatusCode.InternalServerError,
                "Push er ikke sat op: Push:PublicKey, Push:PrivateKey og Push:Subject skal være sat.");
        }

        if (!PushEndpoints.IsAllowed(request.Endpoint))
        {
            throw new PushException(HttpStatusCode.BadRequest,
                $"Abonnementet peger ikke på en kendt push-tjeneste ({PushEndpoints.HostOf(request.Endpoint)}).");
        }

        var host = PushEndpoints.HostOf(request.Endpoint);
        using var vapid = CreateVapid(publicKey, privateKey, subject);
        var client = new PushServiceClient(httpClientFactory.CreateClient(HttpClientName))
        {
            DefaultAuthentication = vapid,
            DefaultAuthenticationScheme = VapidAuthenticationScheme.Vapid,
            // Køen (NotificationWorker) står for genforsøg. Biblioteket må ikke vente selv.
            AutoRetryAfter = false
        };

        var subscription = new WebPushSubscription { Endpoint = request.Endpoint };
        subscription.SetKey(PushEncryptionKeyName.P256DH, request.P256dh);
        subscription.SetKey(PushEncryptionKeyName.Auth, request.Auth);

        var payload = JsonSerializer.Serialize(new { title = request.Title, body = request.Body, url = request.Url, tag = request.Tag }, JsonOptions);
        var message = new PushMessage(payload)
        {
            TimeToLive = request.TimeToLiveSeconds,
            Urgency = request.Urgency == PushUrgency.High ? PushMessageUrgency.High : PushMessageUrgency.Normal
        };

        try
        {
            await client.RequestPushMessageDeliveryAsync(subscription, message, cancellationToken);
            return new SendPushResponseDto { Status = PushDeliveryStatus.Delivered };
        }
        catch (PushServiceClientException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            return new SendPushResponseDto { Status = PushDeliveryStatus.SubscriptionGone };
        }
        catch (PushServiceClientException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new PushException(HttpStatusCode.BadGateway,
                $"{host} afviste servicens VAPID-nøgle ({(int)ex.StatusCode}). Tjek Push:PublicKey, Push:PrivateKey og Push:Subject.", ex.StatusCode);
        }
        catch (PushServiceClientException ex)
        {
            throw new PushException(ex.StatusCode, $"{host} svarede {(int)ex.StatusCode}: {ex.Message}", ex.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            throw new PushException(HttpStatusCode.BadGateway, $"Kunne ikke forbinde til push-tjenesten {host}: {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PushException(HttpStatusCode.GatewayTimeout, $"Push-tjenesten {host} svarede ikke i tide.");
        }
        catch (ArgumentException ex)
        {
            // Biblioteket afviser abonnementets nøgler (p256dh/auth), fx hvis de er ødelagte.
            throw new PushException(HttpStatusCode.BadRequest, $"Abonnementet hos {host} har ugyldige nøgler: {ex.Message}");
        }
    }

    private static VapidAuthentication CreateVapid(string publicKey, string privateKey, string subject)
    {
        try
        {
            return new VapidAuthentication(publicKey, privateKey) { Subject = subject };
        }
        catch (ArgumentException)
        {
            // Beskeden må ikke indeholde nøglerne.
            throw new PushException(HttpStatusCode.InternalServerError,
                "Push:PublicKey, Push:PrivateKey eller Push:Subject er ugyldig. Lav et nyt nøglepar med --generate-vapid-keys, og sæt Push:Subject til mailto:<e-mail> eller https://<adresse>.");
        }
    }

    private bool TryGetSettings(out string publicKey, out string privateKey, out string subject)
    {
        publicKey = configuration["Push:PublicKey"] ?? string.Empty;
        privateKey = configuration["Push:PrivateKey"] ?? string.Empty;
        subject = configuration["Push:Subject"] ?? string.Empty;
        return !string.IsNullOrWhiteSpace(publicKey) && !string.IsNullOrWhiteSpace(privateKey) && !string.IsNullOrWhiteSpace(subject);
    }
}
