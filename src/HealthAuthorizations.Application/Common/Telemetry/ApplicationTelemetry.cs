using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HealthAuthorizations.Application.Common.Telemetry;

/// <summary>
/// Trazas y métricas de negocio con las APIs estándar de .NET (ActivitySource / Meter).
/// Application no depende de OpenTelemetry: la API decide si exporta y hacia dónde.
/// </summary>
public sealed class ApplicationTelemetry
{
    public const string Nombre = "HealthAuthorizations";

    /// <summary>Fuente de spans por caso de uso; se registra en OpenTelemetry por su nombre.</summary>
    public static readonly ActivitySource ActivitySource = new(Nombre);

    private readonly Counter<long> _creadas;
    private readonly Counter<long> _reintentos;
    private readonly Counter<long> _actualizadas;
    private readonly Counter<long> _inactivadas;
    private readonly Histogram<double> _montoReclamado;

    public ApplicationTelemetry(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        var meter = meterFactory.Create(Nombre);

        _creadas = meter.CreateCounter<long>("healthauth.autorizaciones.creadas", "{autorizacion}", "Autorizaciones registradas.");
        _reintentos = meter.CreateCounter<long>("healthauth.autorizaciones.reintentos_idempotentes", "{solicitud}", "Solicitudes repetidas resueltas por idempotencia.");
        _actualizadas = meter.CreateCounter<long>("healthauth.autorizaciones.actualizadas", "{autorizacion}", "Autorizaciones modificadas.");
        _inactivadas = meter.CreateCounter<long>("healthauth.autorizaciones.inactivadas", "{autorizacion}", "Autorizaciones inactivadas.");
        _montoReclamado = meter.CreateHistogram<double>("healthauth.autorizaciones.monto_reclamado", "DOP", "Monto reclamado por autorización.");
    }

    public void AutorizacionCreada(decimal montoReclamado)
    {
        _creadas.Add(1);
        _montoReclamado.Record((double)montoReclamado);
    }

    public void ReintentoIdempotente() => _reintentos.Add(1);

    public void AutorizacionActualizada() => _actualizadas.Add(1);

    public void AutorizacionInactivada() => _inactivadas.Add(1);
}
