using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Autorizaciones.Events;

public sealed record AutorizacionCreadaEvent(Autorizacion Autorizacion) : IDomainEvent;
