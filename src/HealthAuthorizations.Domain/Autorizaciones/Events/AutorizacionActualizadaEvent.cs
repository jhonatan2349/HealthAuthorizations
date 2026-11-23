using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Autorizaciones.Events;

public sealed record AutorizacionActualizadaEvent(Autorizacion Autorizacion) : IDomainEvent;
