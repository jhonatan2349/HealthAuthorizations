using System.Text.Json;
using HealthAuthorizations.Infrastructure.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthAuthorizations.Api.Observability;

/// <summary>
/// Sondas para Kubernetes:
/// <list type="bullet">
/// <item><c>/health/live</c>: el proceso responde (no consulta dependencias; si falla, Kubernetes reinicia el pod).</item>
/// <item><c>/health/ready</c>: puede atender tráfico (SQL Server disponible; si falla, se saca del balanceo sin reiniciar).</item>
/// </list>
/// Son anónimas y sin rate limiting, y no exponen detalles internos.
/// </summary>
internal static class HealthEndpoints
{
    public const string Live = "/health/live";
    public const string Ready = "/health/ready";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static WebApplication MapApiHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks(Live, new HealthCheckOptions { Predicate = _ => false, ResponseWriter = EscribirAsync })
            .AllowAnonymous()
            .DisableRateLimiting();

        app.MapHealthChecks(Ready, new HealthCheckOptions { Predicate = c => c.Tags.Contains(HealthCheckTags.Ready), ResponseWriter = EscribirAsync })
            .AllowAnonymous()
            .DisableRateLimiting();

        return app;
    }

    private static Task EscribirAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";

        var cuerpo = new
        {
            status = report.Status.ToString(),
            duracionMs = (long)report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new { status = e.Value.Status.ToString(), duracionMs = (long)e.Value.Duration.TotalMilliseconds, descripcion = e.Value.Description }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, Json), context.RequestAborted);
    }
}
