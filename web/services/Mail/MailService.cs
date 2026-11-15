using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using MailKit;
using IMailService = web.Services.Mail.Interfaces.IMailService;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using web.Services.Mail.Dtos.Messages;

namespace web.Services.Mail;

/// <summary>
/// E-mail via SMTP (MailKit). Light pattern uden HTTP: konfigurationen læses ved hvert kald, og alle bibliotekets fejl
/// oversættes til MailException i RunAsync. Med Mail:PickupDirectory gemmes mails som .eml-filer i stedet (kun udvikling).
/// </summary>
public partial class MailService(IConfiguration configuration, IHostEnvironment environment) : IMailService
{
    private const int TimeoutMilliseconds = 30_000;

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(configuration["Mail:FromAddress"]) &&
                        (!string.IsNullOrWhiteSpace(configuration["Mail:PickupDirectory"]) ||
                         !string.IsNullOrWhiteSpace(configuration["Mail:Smtp:Host"])));

    public async Task<SendMailResponseDto> SendAsync(SendMailRequestDto request, CancellationToken cancellationToken = default)
    {
        var message = CreateMessage(request);

        var pickup = configuration["Mail:PickupDirectory"];
        if (!string.IsNullOrWhiteSpace(pickup))
        {
            await WriteToPickupAsync(message, pickup, cancellationToken);
            return new SendMailResponseDto { MessageId = message.MessageId, Delivery = MailDelivery.Pickup };
        }

        var host = configuration["Mail:Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new MailException(HttpStatusCode.InternalServerError, "Mail:Smtp:Host er ikke sat (eller Mail:PickupDirectory til udvikling).");
        }

        var security = ParseSecurity(configuration["Mail:Smtp:Security"]);
        var port = int.TryParse(configuration["Mail:Smtp:Port"], out var configuredPort) && configuredPort > 0
            ? configuredPort
            : security == SecureSocketOptions.SslOnConnect ? 465 : 587;
        var userName = configuration["Mail:Smtp:UserName"];
        var password = configuration["Mail:Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(userName) && string.IsNullOrEmpty(password))
        {
            throw new MailException(HttpStatusCode.InternalServerError, "Mail:Smtp:Password er ikke sat (Mail:Smtp:UserName er sat).");
        }

        await RunAsync(host, port, security, async () =>
        {
            using var client = new SmtpClient { Timeout = TimeoutMilliseconds };
            await client.ConnectAsync(host, port, security, cancellationToken);
            if (!string.IsNullOrWhiteSpace(userName))
            {
                await client.AuthenticateAsync(userName, password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }, cancellationToken);

        return new SendMailResponseDto { MessageId = message.MessageId, Delivery = MailDelivery.Smtp };
    }

    private MimeMessage CreateMessage(SendMailRequestDto request)
    {
        var fromAddress = configuration["Mail:FromAddress"];
        if (string.IsNullOrWhiteSpace(fromAddress) || !MailboxAddress.TryParse(fromAddress, out _))
        {
            throw new MailException(HttpStatusCode.InternalServerError, "Mail:FromAddress er ikke sat eller er ikke en gyldig e-mailadresse.");
        }

        if (!MailboxAddress.TryParse(request.ToAddress, out var to) || string.IsNullOrWhiteSpace(to.Address) || !to.Address.Contains('@'))
        {
            throw new MailException(HttpStatusCode.BadRequest, "Modtagerens e-mailadresse er ugyldig.");
        }

        var fromName = configuration["Mail:FromName"];
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(string.IsNullOrWhiteSpace(fromName) ? "Vagttilmelding" : fromName, fromAddress));
        message.To.Add(new MailboxAddress(request.ToName, to.Address));
        message.Subject = request.Subject;
        // Beder autosvar (fx "ude af kontoret") om ikke at svare. Almindelige svar fra forældre lander i systemets postkasse.
        message.Headers.Add("Auto-Submitted", "auto-generated");
        message.Body = new BodyBuilder { TextBody = request.TextBody, HtmlBody = request.HtmlBody }.ToMessageBody();
        return message;
    }

    private async Task WriteToPickupAsync(MimeMessage message, string pickup, CancellationToken cancellationToken)
    {
        var directory = Path.IsPathRooted(pickup) ? pickup : Path.Combine(environment.ContentRootPath, pickup);
        try
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.eml");
            await message.WriteToAsync(path, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new MailException(HttpStatusCode.InternalServerError, $"Kunne ikke gemme mailen i Mail:PickupDirectory: {ex.Message}");
        }
    }

    private static SecureSocketOptions ParseSecurity(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? SecureSocketOptions.StartTls
            : Enum.TryParse<SecureSocketOptions>(value, ignoreCase: true, out var parsed)
                ? parsed
                : throw new MailException(HttpStatusCode.InternalServerError,
                    $"Mail:Smtp:Security har en ukendt værdi. Brug StartTls, SslOnConnect eller None.");

    // Alle SMTP-kald går herigennem, så bibliotekets fejl oversættes ét sted (services.md regel 15).
    private static async Task RunAsync(string host, int port, SecureSocketOptions security, Func<Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await action();
        }
        catch (MailException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException)
        {
            throw new MailException(HttpStatusCode.GatewayTimeout, $"SMTP-serveren {host}:{port} svarede ikke inden for {TimeoutMilliseconds / 1000} sekunder.");
        }
        catch (AuthenticationException)
        {
            throw new MailException(HttpStatusCode.BadGateway, $"SMTP-serveren {host} afviste login. Tjek Mail:Smtp:UserName og Mail:Smtp:Password.");
        }
        catch (SmtpCommandException ex) when (ex.ErrorCode == SmtpErrorCode.RecipientNotAccepted)
        {
            throw new MailException(HttpStatusCode.BadRequest, $"SMTP-serveren afviste modtageradressen ({(int)ex.StatusCode}).");
        }
        catch (SmtpCommandException ex)
        {
            throw new MailException(HttpStatusCode.BadGateway, $"SMTP-serveren {host} svarede {(int)ex.StatusCode} ({ex.ErrorCode}): {MaskAddresses(ex.Message)}");
        }
        catch (Exception ex) when (ex is SmtpProtocolException or ServiceNotAuthenticatedException or ServiceNotConnectedException)
        {
            throw new MailException(HttpStatusCode.BadGateway, $"Fejl i forbindelsen til SMTP-serveren {host}: {MaskAddresses(ex.Message)}");
        }
        catch (Exception ex) when (ex is SocketException or SslHandshakeException or IOException)
        {
            throw new MailException(HttpStatusCode.BadGateway,
                $"Kunne ikke forbinde til SMTP-serveren {host}:{port} ({security}). Tjek Mail:Smtp:Host, Mail:Smtp:Port og Mail:Smtp:Security. {ex.Message}");
        }
    }

    /// <summary>Fjerner e-mailadresser fra serverens svar, så de ikke havner i loggen (persondata).</summary>
    private static string MaskAddresses(string text) => AddressRegex().Replace(text, "<adresse>");

    [GeneratedRegex(@"[^\s<>""'@]+@[^\s<>""'@]+")]
    private static partial Regex AddressRegex();
}
