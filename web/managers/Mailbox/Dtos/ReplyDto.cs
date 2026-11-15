using web.Services.Mail.Dtos.Mailbox;

namespace web.Managers.Mailbox.Dtos;

/// <summary>Svaret, admin sender. Modtager og trådens headers tages fra den oprindelige mail, ikke fra formularen.</summary>
public class ReplyDto
{
    public MailFolder Folder { get; set; }
    public uint Uid { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
