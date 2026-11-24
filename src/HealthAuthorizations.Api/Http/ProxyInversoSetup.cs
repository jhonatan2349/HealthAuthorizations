using IPNetwork = System.Net.IPNetwork;
using Microsoft.AspNetCore.HttpOverrides;

namespace HealthAuthorizations.Api.Http;

/// <summary>
/// Confianza en el proxy inverso o ingress que termina TLS (sección "ProxyInverso").
/// Solo se aceptan X-Forwarded-For/Proto que lleguen desde las redes declaradas: de lo contrario cualquier
/// cliente podría falsear su IP (y evadir el rate limiting por IP) o simular HTTPS.
/// </summary>
public sealed class ProxyInversoOptions
{
    public const string SectionName = "ProxyInverso";

    public bool Habilitado { get; init; }

    /// <summary>Redes en notación CIDR desde las que llegan los proxies (p. ej. la red de pods del clúster).</summary>
    public IReadOnlyList<string> RedesConfiables { get; init; } = [];

    /// <summary>Saltos de proxy que se procesan de derecha a izquierda (1 = solo el ingress).</summary>
    public int MaximoSaltos { get; init; } = 1;
}

internal static class ProxyInversoSetup
{
    public static IServiceCollection AddApiProxyInverso(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ProxyInversoOptions>()
            .BindConfiguration(ProxyInversoOptions.SectionName)
            .Validate(o => !o.Habilitado || o.RedesConfiables.Count > 0,
                "ProxyInverso habilitado sin RedesConfiables: se confiaría en cualquier origen.")
            .Validate(o => o.RedesConfiables.All(r => IPNetwork.TryParse(r, out _)),
                "ProxyInverso:RedesConfiables debe contener redes CIDR válidas (p. ej. 10.0.0.0/8).")
            .Validate(o => o.MaximoSaltos is >= 1 and <= 5, "ProxyInverso:MaximoSaltos debe estar entre 1 y 5.")
            .ValidateOnStart();

        var opciones = configuration.GetSection(ProxyInversoOptions.SectionName).Get<ProxyInversoOptions>() ?? new();

        return services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = opciones.MaximoSaltos;

            // Por defecto ASP.NET Core confía en loopback; aquí solo en lo declarado.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var red in opciones.RedesConfiables)
            {
                if (IPNetwork.TryParse(red, out var redValida))
                {
                    options.KnownIPNetworks.Add(redValida);
                }
            }
        });
    }

    /// <summary>Debe ir primero: todo lo que sigue (rate limiting, HTTPS, logs) depende de la IP y el esquema reales.</summary>
    public static IApplicationBuilder UseApiProxyInverso(this WebApplication app)
    {
        var opciones = app.Configuration.GetSection(ProxyInversoOptions.SectionName).Get<ProxyInversoOptions>();

        return opciones?.Habilitado == true ? app.UseForwardedHeaders() : app;
    }
}
