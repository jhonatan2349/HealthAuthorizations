using HealthAuthorizations.Application.Autorizaciones.Actualizar;
using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Autorizaciones.Crear;
using HealthAuthorizations.Application.Autorizaciones.Inactivar;

namespace HealthAuthorizations.Api.Contracts;

/// <summary>Cuerpo de POST /autorizaciones. La clave de idempotencia viaja en el header Idempotency-Key.</summary>
/// <param name="AfiliadoId">Afiliado beneficiario; su contratante e intermediario se toman automáticamente.</param>
/// <param name="FechaServicio">Fecha del servicio (yyyy-MM-dd); hasta 30 días en el futuro.</param>
/// <param name="Servicios">Entre 1 y 50 servicios con su monto reclamado.</param>
public sealed record CrearAutorizacionRequest(
    int AfiliadoId,
    int PrestadorId,
    DateOnly FechaServicio,
    string? Diagnostico,
    string? Observaciones,
    IReadOnlyList<ServicioRequest> Servicios)
{
    internal CrearAutorizacionCommand ToCommand(string? claveIdempotencia) =>
        new(AfiliadoId, PrestadorId, FechaServicio, Diagnostico, Observaciones, Servicios ?? [], claveIdempotencia);
}

/// <summary>Cuerpo de PUT /autorizaciones/{id}. Reemplaza los datos y servicios; el afiliado no cambia.</summary>
public sealed record ActualizarAutorizacionRequest(
    int PrestadorId,
    DateOnly FechaServicio,
    string? Diagnostico,
    string? Observaciones,
    IReadOnlyList<ServicioRequest> Servicios)
{
    internal ActualizarAutorizacionCommand ToCommand(long id, string? version) =>
        new(id, PrestadorId, FechaServicio, Diagnostico, Observaciones, Servicios ?? [], version);
}

/// <summary>Cuerpo de POST /autorizaciones/{id}/inactivacion.</summary>
public sealed record InactivarAutorizacionRequest(string Motivo)
{
    internal InactivarAutorizacionCommand ToCommand(long id, string? version) => new(id, Motivo, version);
}
