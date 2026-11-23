using HealthAuthorizations.Api.ErrorHandling;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace HealthAuthorizations.Api.Observability;

/// <summary>Límites de tiempo (sección "Resiliencia").</summary>
public sealed class ResilienciaOptions
{
    public const string SectionName = "Resiliencia";

    /// <summary>Tiempo máximo de una solicitud; al vencer se cancela el trabajo (incluidas las consultas) y se responde 504.</summary>
    public int TimeoutSolicitudSegundos { get; init; } = 30;

    /// <summary>Tiempo para terminar las solicitudes en curso al apagar (debe ser menor que terminationGracePeriodSeconds).</summary>
    public int TimeoutApagadoSegundos { get; init; } = 25;
}

internal static class ResilienceSetup
{
    public static WebApplicationBuilder AddApiResilience(this WebApplicationBuilder builder)
    {
        var opciones = builder.Configuration.GetSection(ResilienciaOptions.SectionName).Get<ResilienciaOptions>() ?? new ResilienciaOptions();

        // Evita que una solicitud lenta retenga hilos y conexiones indefinidamente: el CancellationToken
        // propagado hasta EF Core cancela también la consulta en SQL Server.
        builder.Services.AddRequestTimeouts(options => options.DefaultPolicy = new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(opciones.TimeoutSolicitudSegundos),
            TimeoutStatusCode = StatusCodes.Status504GatewayTimeout,
            WriteTimeoutResponse = EscribirTimeoutAsync,
        });

        // Apagado ordenado: al recibir SIGTERM se dejan de aceptar conexiones y se terminan las solicitudes en curso.
        builder.Services.Configure<HostOptions>(host => host.ShutdownTimeout = TimeSpan.FromSeconds(opciones.TimeoutApagadoSegundos));

        return builder;
    }

    private static Task EscribirTimeoutAsync(HttpContext context) =>
        context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status504GatewayTimeout,
                Title = "Tiempo de espera agotado",
                Detail = "La operación tardó más de lo permitido y fue cancelada. Intente de nuevo.",
                Extensions = { [ProblemTitles.ExtensionCodigo] = "Solicitud.TiempoAgotado" },
            },
        }).AsTask();
}
