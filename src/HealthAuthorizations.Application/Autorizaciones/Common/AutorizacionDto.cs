using HealthAuthorizations.Domain.Autorizaciones;

namespace HealthAuthorizations.Application.Autorizaciones.Common;

/// <summary>Referencia a una entidad relacionada con su código y nombre legible.</summary>
public sealed record ReferenciaDto(int Id, string Codigo, string Nombre);

public sealed record ServicioAutorizadoDto(
    long Id,
    ReferenciaDto TipoServicio,
    string Descripcion,
    decimal MontoReclamado,
    decimal PorcentajeCobertura,
    decimal MontoArs,
    decimal MontoAfiliado);

/// <summary>Detalle completo de una autorización. <c>Version</c> es el token de concurrencia (ETag).</summary>
public sealed record AutorizacionDto(
    long Id,
    string Numero,
    EstadoAutorizacion Estado,
    DateOnly FechaServicio,
    string? Diagnostico,
    string? Observaciones,
    ReferenciaDto Afiliado,
    ReferenciaDto Prestador,
    ReferenciaDto Contratante,
    ReferenciaDto? Intermediario,
    decimal MontoReclamado,
    decimal MontoArs,
    decimal MontoAfiliado,
    IReadOnlyList<ServicioAutorizadoDto> Servicios,
    DateTimeOffset? FechaInactivacion,
    string? MotivoInactivacion,
    DateTimeOffset CreadoEn,
    string CreadoPor,
    DateTimeOffset? ModificadoEn,
    string? ModificadoPor,
    string Version);
