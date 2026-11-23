namespace HealthAuthorizations.Domain.Common;

public abstract class AuditableEntity<TId> : Entity<TId>, IAuditable
    where TId : struct
{
    public const int MaxLongitudUsuario = 100;

    public DateTimeOffset CreadoEn { get; private set; }

    public string CreadoPor { get; private set; } = string.Empty;

    public DateTimeOffset? ModificadoEn { get; private set; }

    public string? ModificadoPor { get; private set; }
}
