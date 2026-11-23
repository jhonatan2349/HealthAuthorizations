namespace HealthAuthorizations.Domain.Common;

/// <summary>
/// Raíz de agregado: frontera de consistencia y único punto de entrada para modificar sus entidades hijas.
/// </summary>
public abstract class AggregateRoot<TId> : AuditableEntity<TId>
    where TId : struct
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
