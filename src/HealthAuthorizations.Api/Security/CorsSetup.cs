using HealthAuthorizations.Api.Controllers;
using HealthAuthorizations.Api.Middleware;
using Microsoft.Net.Http.Headers;

namespace HealthAuthorizations.Api.Security;

/// <summary>
/// CORS cerrado por defecto: la API la consumen otros sistemas (servidor a servidor).
/// Solo se habilita para los orígenes listados en "Cors:OrigenesPermitidos".
/// </summary>
internal static class CorsSetup
{
    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origenes = configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];

        return services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            if (origenes.Length == 0)
            {
                return;
            }

            policy.WithOrigins(origenes)
                .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put)
                .WithHeaders(
                    HeaderNames.Authorization,
                    HeaderNames.ContentType,
                    HeaderNames.IfMatch,
                    HeaderNames.IfNoneMatch,
                    AutorizacionesController.IdempotencyKeyHeader,
                    CorrelationIdMiddleware.HeaderName)
                .WithExposedHeaders(
                    HeaderNames.ETag,
                    HeaderNames.Location,
                    HeaderNames.RetryAfter,
                    AutorizacionesController.IdempotentReplayedHeader,
                    CorrelationIdMiddleware.HeaderName);
        }));
    }
}
