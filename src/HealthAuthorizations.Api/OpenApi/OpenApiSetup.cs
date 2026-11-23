using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace HealthAuthorizations.Api.OpenApi;

internal static class OpenApiSetup
{
    public const string DocumentName = "v1";
    private const string EsquemaBearer = "Bearer";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services) =>
        services.AddOpenApi(DocumentName, options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "HealthAuthorizations API",
                Version = DocumentName,
                Description =
                    "API REST para gestionar autorizaciones de servicios de salud: registro, consulta, modificación e inactivación, " +
                    "con distribución de montos ARS/afiliado.\n\n" +
                    "- Errores en formato ProblemDetails (RFC 9457) con `code` estable y `traceId`.\n" +
                    "- `Idempotency-Key` en POST para evitar duplicados por reintentos.\n" +
                    "- `ETag` / `If-Match` para concurrencia optimista.\n" +
                    "- Listados paginados por cursor (`nextCursor`).\n\n" +
                    "**Autenticación:** JWT Bearer. En la demo obtenga un token con `POST /api/v1/auth/token` " +
                    "(usuarios `lector` y `operador`) y péguelo en *Authentication*.",
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[EsquemaBearer] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token JWT con rol autorizaciones.lector (consulta) o autorizaciones.operador (consulta y escritura).",
            };
            document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(EsquemaBearer, document)] = [] }];

            return Task.CompletedTask;
        }));

    /// <summary>Expone el documento OpenAPI (/openapi/v1.json) y la interfaz Scalar (/scalar).</summary>
    public static WebApplication MapApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference(options => options
                .WithTitle("HealthAuthorizations API")
                .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl)
                .AddPreferredSecuritySchemes(EsquemaBearer))
            .AllowAnonymous();
        return app;
    }
}
