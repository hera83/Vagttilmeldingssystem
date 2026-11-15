using web.Services.Mail.Dtos.Messages;

namespace web.Services.Mail.Interfaces;

public interface IMailService
{
    /// <summary>Kan der sendes mails? Mail:FromAddress og enten Mail:Smtp:Host eller Mail:PickupDirectory skal være sat.</summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>Sender én mail fra systemets postkasse (Mail:FromAddress).</summary>
    Task<SendMailResponseDto> SendAsync(SendMailRequestDto request, CancellationToken cancellationToken = default);
}
