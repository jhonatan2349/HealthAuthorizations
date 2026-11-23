namespace HealthAuthorizations.Domain.Common;

/// <summary>Acceso no genérico a los eventos de dominio de un agregado (usado al persistir).</summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

/// <summary>
/// Raíz de agregado: frontera de consistencia y único punto de entrada para modificar sus entidades hijas.
/// </summary>
public abstract class AggregateRoot<TId> : AuditableEntity<TId>, IHasDomainEvents
    where TId : struct
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
