using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Common.Results;
using MediatR;

namespace HealthAuthorizations.Application.Autorizaciones.Crear;

/// <param name="ClaveIdempotencia">Valor del header Idempotency-Key; reintentos con la misma clave devuelven la misma autorización.</param>
public sealed record CrearAutorizacionCommand(
    int AfiliadoId,
    int PrestadorId,
    DateOnly FechaServicio,
    string? Diagnostico,
    string? Observaciones,
    IReadOnlyList<ServicioRequest> Servicios,
    string? ClaveIdempotencia = null) : IRequest<Result<CrearAutorizacionResult>>, IDatosAutorizacion;

/// <param name="EsReintento">true cuando la clave de idempotencia ya existía y se devuelve la autorización original.</param>
public sealed record CrearAutorizacionResult(AutorizacionDto Autorizacion, bool EsReintento);
