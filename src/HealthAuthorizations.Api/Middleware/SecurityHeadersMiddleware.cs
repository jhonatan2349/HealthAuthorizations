using Microsoft.Net.Http.Headers;

namespace HealthAuthorizations.Api.Middleware;

/// <summary>
/// Headers de seguridad para una API JSON. La interfaz de documentación (/scalar) necesita scripts,
/// por eso queda fuera de la CSP restrictiva. Las respuestas contienen datos de salud: solo el cliente
/// puede guardarlas en caché y siempre revalidando (compatible con ETag / If-None-Match).
/// </summary>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string RutaDocumentacion = "/scalar";

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";

            if (!context.Request.Path.StartsWithSegments(RutaDocumentacion, StringComparison.OrdinalIgnoreCase))
            {
                headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            }

            if (!headers.ContainsKey(HeaderNames.CacheControl))
            {
                headers.CacheControl = "private, no-cache";
            }

            return Task.CompletedTask;
        });

        return next(context);
    }
}
