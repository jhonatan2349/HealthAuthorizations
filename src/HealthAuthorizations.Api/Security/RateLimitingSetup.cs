using System.Globalization;
using System.Threading.RateLimiting;
using HealthAuthorizations.Api.ErrorHandling;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace HealthAuthorizations.Api.Security;

/// <summary>Límites de solicitudes por minuto (sección "RateLimiting").</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Por usuario autenticado (claim sub) o, si es anónimo, por IP.</summary>
    public int PermisosPorMinuto { get; init; } = 300;

    /// <summary>Emisión de tokens por IP: bajo, para frenar ataques de fuerza bruta.</summary>
    public int PermisosTokenPorMinuto { get; init; } = 10;
}

internal static class RateLimitingSetup
{
    public const string PoliticaToken = "token";
    private static readonly TimeSpan Ventana = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
            .Validate(o => o.PermisosPorMinuto > 0 && o.PermisosTokenPorMinuto > 0, "Los límites de RateLimiting deben ser mayores que cero.")
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global: protege a la API (y a la BD) de un cliente que acapare recursos.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                CrearVentana(Particion(context), Opciones(context).PermisosPorMinuto));

            options.AddPolicy(PoliticaToken, context =>
                CrearVentana($"token:{context.Connection.RemoteIpAddress}", Opciones(context).PermisosTokenPorMinuto));

            options.OnRejected = EscribirRechazoAsync;
        });

        return services;
    }

    private static RateLimitingOptions Opciones(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

    private static string Particion(HttpContext context) =>
        context.User.FindFirst(Claims.Subject)?.Value is { } sujeto
            ? $"usuario:{sujeto}"
            : $"ip:{context.Connection.RemoteIpAddress}";

    private static RateLimitPartition<string> CrearVentana(string clave, int permisos) =>
        RateLimitPartition.GetFixedWindowLimiter(clave, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permisos,
            Window = Ventana,
            QueueLimit = 0,
        });

    private static async ValueTask EscribirRechazoAsync(OnRejectedContext rechazo, CancellationToken cancellationToken)
    {
        var http = rechazo.HttpContext;
        var segundos = rechazo.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)Math.Ceiling(retryAfter.TotalSeconds)
            : (int)Ventana.TotalSeconds;

        http.Response.Headers.RetryAfter = segundos.ToString(CultureInfo.InvariantCulture);

        await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Demasiadas solicitudes",
                Detail = $"Se superó el límite de solicitudes. Intente de nuevo en {segundos} segundos.",
                Extensions = { [ProblemTitles.ExtensionCodigo] = "Solicitudes.Excedidas" },
            },
        });
    }
}
