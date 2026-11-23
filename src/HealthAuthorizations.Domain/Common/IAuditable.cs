namespace HealthAuthorizations.Domain.Common;

/// <summary>
/// Marca las entidades cuyos campos de auditoría se completan de forma automática al persistir.
/// Los valores los asigna la infraestructura; el dominio solo los expone para lectura.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreadoEn { get; }

    string CreadoPor { get; }

    DateTimeOffset? ModificadoEn { get; }

    string? ModificadoPor { get; }
}
