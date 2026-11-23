namespace HealthAuthorizations.Domain.Common;

/// <summary>Entidad con identidad.</summary>
public abstract class Entity<TId>
    where TId : struct
{
    public TId Id { get; protected set; }
}
