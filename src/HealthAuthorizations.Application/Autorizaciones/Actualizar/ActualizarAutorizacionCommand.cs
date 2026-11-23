using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Common.Results;
using MediatR;

namespace HealthAuthorizations.Application.Autorizaciones.Actualizar;

/// <summary>Reemplaza los datos modificables y los servicios. El afiliado no cambia.</summary>
/// <param name="Version">Valor de If-Match; si se envía y no coincide, la operación falla por concurrencia.</param>
public sealed record ActualizarAutorizacionCommand(
    long Id,
    int PrestadorId,
    DateOnly FechaServicio,
    string? Diagnostico,
    string? Observaciones,
    IReadOnlyList<ServicioRequest> Servicios,
    string? Version = null) : IRequest<Result<AutorizacionDto>>, IDatosAutorizacion;
