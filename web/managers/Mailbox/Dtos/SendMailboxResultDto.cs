namespace web.Managers.Mailbox.Dtos;

public class SendMailboxResultDto
{
    public SendMailboxStatus Status { get; set; }

    /// <summary>Forklaring ved Failed/InvalidRecipient.</summary>
    public string? Error { get; set; }

    /// <summary>Mailen er sendt, men kopien kunne ikke gemmes i Sendt.</summary>
    public string? SentCopyError { get; set; }
}
