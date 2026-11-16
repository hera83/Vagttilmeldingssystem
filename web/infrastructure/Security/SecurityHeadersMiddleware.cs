namespace web.Infrastructure.Security;

/// <summary>
/// Sikkerheds-headers på alle svar (step 15). CSP tillader kun appens egne filer: ingen inline-scripts eller -styles
/// (der er ingen), ingen fremmede billeder eller fonte, og live-opdatering (SignalR) kun til samme host.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            var host = context.Request.Host.Value;
            headers.ContentSecurityPolicy =
                "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; " +
                $"connect-src 'self' wss://{host} ws://{host}; manifest-src 'self'; worker-src 'self'; " +
                "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "same-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            return Task.CompletedTask;
        });

        return next(context);
    }
}
