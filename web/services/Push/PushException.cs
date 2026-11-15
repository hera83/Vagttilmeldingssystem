using System.Net;

namespace web.Services.Push;

/// <summary>Fejl fra push-tjenesten (eller manglende konfiguration). Se GlobalExceptionHandler.</summary>
public class PushException(
    HttpStatusCode statusCode,
    string message,
    HttpStatusCode? upstreamStatusCode = null) : Exception(message)
{
    /// <summary>Statuskoden, appen svarer med.</summary>
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>Statuskoden fra push-tjenesten, eller null hvis den ikke kunne nås.</summary>
    public HttpStatusCode? UpstreamStatusCode { get; } = upstreamStatusCode;
}
