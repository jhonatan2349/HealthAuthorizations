using HealthAuthorizations.Api.Security;
using HealthAuthorizations.Application.Catalogos;
using HealthAuthorizations.Application.Common.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthAuthorizations.Api.Controllers;

/// <summary>Catálogos de solo lectura necesarios para registrar autorizaciones.</summary>
[Route("api/v1/catalogos")]
[Authorize(Policy = Politicas.Lectura)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class CatalogosController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Tipos de servicio autorizables (consulta, emergencia, hospitalización, laboratorio, imágenes).</summary>
    [HttpGet("tipos-servicio")]
    [ProducesResponseType<IReadOnlyList<CatalogoDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CatalogoDto>> TiposServicio([FromQuery] bool incluirInactivos, CancellationToken cancellationToken) =>
        Listar(TipoCatalogo.TiposServicio, incluirInactivos, cancellationToken);

    /// <summary>Tipos de prestador (médico, farmacia, clínica, laboratorio).</summary>
    [HttpGet("tipos-prestador")]
    [ProducesResponseType<IReadOnlyList<CatalogoDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CatalogoDto>> TiposPrestador([FromQuery] bool incluirInactivos, CancellationToken cancellationToken) =>
        Listar(TipoCatalogo.TiposPrestador, incluirInactivos, cancellationToken);

    /// <summary>Contratantes (clientes) de los planes.</summary>
    [HttpGet("contratantes")]
    [ProducesResponseType<IReadOnlyList<CatalogoDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CatalogoDto>> Contratantes([FromQuery] bool incluirInactivos, CancellationToken cancellationToken) =>
        Listar(TipoCatalogo.Contratantes, incluirInactivos, cancellationToken);

    /// <summary>Intermediarios (corredores y agentes).</summary>
    [HttpGet("intermediarios")]
    [ProducesResponseType<IReadOnlyList<CatalogoDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CatalogoDto>> Intermediarios([FromQuery] bool incluirInactivos, CancellationToken cancellationToken) =>
        Listar(TipoCatalogo.Intermediarios, incluirInactivos, cancellationToken);

    /// <summary>Prestadores, filtrables por tipo y búsqueda, paginados por cursor.</summary>
    [HttpGet("prestadores")]
    [ProducesResponseType<CursorPage<PrestadorDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<CursorPage<PrestadorDto>> Prestadores([FromQuery] ListarPrestadoresQuery query, CancellationToken cancellationToken) =>
        await Sender.Send(query, cancellationToken);

    /// <summary>Búsqueda de afiliados por número, documento o nombre, paginada por cursor.</summary>
    [HttpGet("afiliados")]
    [ProducesResponseType<CursorPage<AfiliadoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<CursorPage<AfiliadoDto>> Afiliados([FromQuery] BuscarAfiliadosQuery query, CancellationToken cancellationToken) =>
        await Sender.Send(query, cancellationToken);

    private Task<IReadOnlyList<CatalogoDto>> Listar(TipoCatalogo tipo, bool incluirInactivos, CancellationToken cancellationToken) =>
        Sender.Send(new ListarCatalogoQuery(tipo, incluirInactivos), cancellationToken);
}
