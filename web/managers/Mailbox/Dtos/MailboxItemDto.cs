using web.Services.Mail.Dtos.Mailbox;

namespace web.Managers.Mailbox.Dtos;

public class MailboxItemDto
{
    public MailSummaryDto Summary { get; set; } = new();
    public KnownUserDto? KnownUser { get; set; }
}
