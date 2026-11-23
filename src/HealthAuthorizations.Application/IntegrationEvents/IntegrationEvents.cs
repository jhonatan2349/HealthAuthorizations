using System.Text.Json.Serialization;

namespace HealthAuthorizations.Application.IntegrationEvents;

/// <summary>
/// Contrato público que se publica a otros sistemas. A diferencia de los eventos de dominio,
/// es estable y versionado: un cambio incompatible crea una nueva versión (v2) sin romper a los consumidores.
/// No incluye datos personales ni clínicos (nombres, diagnóstico, motivo): solo identificadores y montos;
/// quien necesite el detalle lo consulta a la API con su propio token.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>Tipo del evento; se usa como routing key. Viaja en el sobre, no en el cuerpo.</summary>
    [JsonIgnore]
    public abstract string Tipo { get; }
}

public sealed record AutorizacionCreadaV1(
    long AutorizacionId,
    string Numero,
    int AfiliadoId,
    int PrestadorId,
    int ContratanteId,
    int? IntermediarioId,
    DateOnly FechaServicio,
    decimal MontoReclamado,
    decimal MontoArs,
    decimal MontoAfiliado,
    int CantidadServicios) : IntegrationEvent
{
    public const string NombreTipo = "autorizacion.creada.v1";

    public override string Tipo => NombreTipo;
}

public sealed record AutorizacionActualizadaV1(
    long AutorizacionId,
    string Numero,
    int PrestadorId,
    DateOnly FechaServicio,
    decimal MontoReclamado,
    decimal MontoArs,
    decimal MontoAfiliado,
    int CantidadServicios) : IntegrationEvent
{
    public const string NombreTipo = "autorizacion.actualizada.v1";

    public override string Tipo => NombreTipo;
}

public sealed record AutorizacionEstadoCambiadoV1(
    long AutorizacionId,
    string Numero,
    string EstadoAnterior,
    string EstadoNuevo) : IntegrationEvent
{
    public const string NombreTipo = "autorizacion.estado-cambiado.v1";

    public override string Tipo => NombreTipo;
}
