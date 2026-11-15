using System.Net;

namespace web.Services.Mail;

/// <summary>Fejl fra mailserveren (eller manglende konfiguration). Se GlobalExceptionHandler.</summary>
public class MailException(HttpStatusCode statusCode, string message) : Exception(message)
{
    /// <summary>Statuskoden, appen svarer med.</summary>
    public HttpStatusCode StatusCode { get; } = statusCode;
}
