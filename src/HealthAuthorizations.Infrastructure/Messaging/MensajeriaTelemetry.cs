using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HealthAuthorizations.Infrastructure.Messaging;

/// <summary>Trazas y métricas de la publicación de eventos.</summary>
public sealed class MensajeriaTelemetry
{
    public const string Nombre = "HealthAuthorizations.Mensajeria";

    public static readonly ActivitySource ActivitySource = new(Nombre);

    private readonly Counter<long> _publicados;
    private readonly Counter<long> _fallidos;
    private readonly Counter<long> _agotados;

    public MensajeriaTelemetry(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        var meter = meterFactory.Create(Nombre);
        _publicados = meter.CreateCounter<long>("healthauth.outbox.publicados", "{mensaje}", "Mensajes publicados y confirmados por el broker.");
        _fallidos = meter.CreateCounter<long>("healthauth.outbox.fallidos", "{intento}", "Intentos de publicación fallidos (se reintentan).");
        _agotados = meter.CreateCounter<long>("healthauth.outbox.agotados", "{mensaje}", "Mensajes que agotaron los reintentos (requieren revisión).");
    }

    public void Publicado(string tipo) => _publicados.Add(1, new KeyValuePair<string, object?>("tipo", tipo));

    public void Fallido(string tipo) => _fallidos.Add(1, new KeyValuePair<string, object?>("tipo", tipo));

    public void Agotado(string tipo) => _agotados.Add(1, new KeyValuePair<string, object?>("tipo", tipo));
}
