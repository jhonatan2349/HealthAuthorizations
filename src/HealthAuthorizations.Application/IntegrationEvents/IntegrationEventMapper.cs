using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.Domain.Autorizaciones.Events;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Application.IntegrationEvents;

/// <summary>
/// Traduce eventos de dominio (internos, pueden cambiar) a eventos de integración (contrato público).
/// Agregar una integración nueva no requiere tocar este mapeo: basta con suscribirse al exchange.
/// </summary>
public static class IntegrationEventMapper
{
    public static IntegrationEvent? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        AutorizacionCreadaEvent e => new AutorizacionCreadaV1(
            e.Autorizacion.Id,
            Autorizacion.FormatearNumero(e.Autorizacion.Id),
            e.Autorizacion.AfiliadoId,
            e.Autorizacion.PrestadorId,
            e.Autorizacion.ContratanteId,
            e.Autorizacion.IntermediarioId,
            e.Autorizacion.FechaServicio,
            e.Autorizacion.MontoReclamado,
            e.Autorizacion.MontoArs,
            e.Autorizacion.MontoAfiliado,
            e.Autorizacion.Detalles.Count),

        AutorizacionActualizadaEvent e => new AutorizacionActualizadaV1(
            e.Autorizacion.Id,
            Autorizacion.FormatearNumero(e.Autorizacion.Id),
            e.Autorizacion.PrestadorId,
            e.Autorizacion.FechaServicio,
            e.Autorizacion.MontoReclamado,
            e.Autorizacion.MontoArs,
            e.Autorizacion.MontoAfiliado,
            e.Autorizacion.Detalles.Count),

        AutorizacionEstadoCambiadoEvent e => new AutorizacionEstadoCambiadoV1(
            e.Autorizacion.Id,
            Autorizacion.FormatearNumero(e.Autorizacion.Id),
            e.EstadoAnterior.ToString(),
            e.EstadoNuevo.ToString()),

        _ => null,
    };
}
