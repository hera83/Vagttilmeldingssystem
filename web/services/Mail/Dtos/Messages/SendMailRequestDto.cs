namespace web.Services.Mail.Dtos.Messages;

public class SendMailRequestDto
{
    public string ToAddress { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;

    /// <summary>Valgfri HTML-udgave. Uden den sendes kun tekst.</summary>
    public string? HtmlBody { get; set; }
}
