using HealthAuthorizations.Api.Contracts;
using HealthAuthorizations.Api.Http;
using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Autorizaciones.Listar;
using HealthAuthorizations.Application.Autorizaciones.Obtener;
using HealthAuthorizations.Application.Common.Pagination;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace HealthAuthorizations.Api.Controllers;

/// <summary>Gestión de autorizaciones de servicios de salud.</summary>
[Route("api/v1/autorizaciones")]
public sealed class AutorizacionesController(ISender sender) : ApiControllerBase(sender)
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string IdempotentReplayedHeader = "Idempotent-Replayed";

    /// <summary>Crea una autorización.</summary>
    /// <remarks>
    /// Envíe un <c>Idempotency-Key</c> único por operación: si la solicitud se repite (timeout, reintento),
    /// se devuelve la misma autorización con el header <c>Idempotent-Replayed: true</c> en lugar de duplicarla.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType<AutorizacionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearAutorizacionRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await Sender.Send(request.ToCommand(idempotencyKey), cancellationToken);
        if (result.IsFailure)
        {
            return Problema(result.Error!);
        }

        var dto = result.Value.Autorizacion;
        Response.Headers.ETag = EntityTagHeader.Formatear(dto.Version);
        if (result.Value.EsReintento)
        {
            Response.Headers[IdempotentReplayedHeader] = "true";
        }

        return CreatedAtAction(nameof(Obtener), new { id = dto.Id }, dto);
    }

    /// <summary>Lista autorizaciones del más reciente al más antiguo, con filtros y paginación por cursor.</summary>
    /// <remarks>Para la siguiente página envíe el <c>nextCursor</c> recibido. No se calcula el total para mantener tiempos constantes.</remarks>
    [HttpGet]
    [ProducesResponseType<CursorPage<AutorizacionResumenDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CursorPage<AutorizacionResumenDto>>> Listar(
        [FromQuery] ListarAutorizacionesQuery query,
        CancellationToken cancellationToken) =>
        Ok(await Sender.Send(query, cancellationToken));

    /// <summary>Obtiene el detalle de una autorización.</summary>
    /// <remarks>Devuelve <c>ETag</c>; con <c>If-None-Match</c> igual a la versión actual responde 304 sin cuerpo.</remarks>
    [HttpGet("{id:long}")]
    [ProducesResponseType<AutorizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(long id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ObtenerAutorizacionQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return Problema(result.Error!);
        }

        var etag = EntityTagHeader.Formatear(result.Value.Version);
        Response.Headers.ETag = etag;

        return Request.Headers[HeaderNames.IfNoneMatch] == etag
            ? StatusCode(StatusCodes.Status304NotModified)
            : Ok(result.Value);
    }

    /// <summary>Actualiza una autorización activa (reemplaza datos y servicios, recalcula montos).</summary>
    /// <remarks>Envíe <c>If-Match</c> con el ETag leído: si otro usuario la modificó, responde 412 en lugar de sobrescribir.</remarks>
    [HttpPut("{id:long}")]
    [ProducesResponseType<AutorizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(
        long id,
        [FromBody] ActualizarAutorizacionRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await Sender.Send(request.ToCommand(id, EntityTagHeader.Leer(ifMatch)), cancellationToken);
        if (result.IsFailure)
        {
            return Problema(result.Error!);
        }

        Response.Headers.ETag = EntityTagHeader.Formatear(result.Value.Version);
        return Ok(result.Value);
    }

    /// <summary>Inactiva una autorización. Se conserva para fines históricos y ya no puede modificarse.</summary>
    [HttpPost("{id:long}/inactivacion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> Inactivar(
        long id,
        [FromBody] InactivarAutorizacionRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await Sender.Send(request.ToCommand(id, EntityTagHeader.Leer(ifMatch)), cancellationToken);
        return result.IsSuccess ? NoContent() : Problema(result.Error!);
    }
}
