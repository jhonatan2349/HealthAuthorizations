using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Autorizaciones.Events;

public sealed record AutorizacionEstadoCambiadoEvent(
    Autorizacion Autorizacion,
    EstadoAutorizacion EstadoAnterior,
    EstadoAutorizacion EstadoNuevo) : IDomainEvent;
