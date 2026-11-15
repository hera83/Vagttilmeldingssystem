using Microsoft.AspNetCore.Diagnostics;
using web.Services.Push;

namespace web.Infrastructure.ErrorHandling;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            // Én linje pr. service med egen exception.
            PushException ex => (int)ex.StatusCode,
            _ => StatusCodes.Status500InternalServerError
        };

        // Request.Path er allerede skrevet om til /Home/Error. Den oprindelige sti ligger i featuren.
        var path = httpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? httpContext.Request.Path.Value;

        logger.Log(statusCode >= StatusCodes.Status500InternalServerError ? LogLevel.Error : LogLevel.Warning,
            exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, path);

        httpContext.Response.StatusCode = statusCode;

        // Websider får altid fejlsiden fra UseExceptionHandler.
        return ValueTask.FromResult(false);
    }
}
