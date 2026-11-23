using HealthAuthorizations.Application.Common.Pagination;
using HealthAuthorizations.Domain.Autorizaciones;
using MediatR;

namespace HealthAuthorizations.Application.Autorizaciones.Listar;

/// <summary>
/// Listado paginado por cursor, del más reciente al más antiguo. Todos los filtros son opcionales y combinables.
/// </summary>
/// <param name="Cursor">Valor <c>NextCursor</c> de la página anterior; vacío para la primera página.</param>
/// <param name="Limite">Tamaño de página (1–100).</param>
public sealed record ListarAutorizacionesQuery(
    string? Numero = null,
    int? AfiliadoId = null,
    string? DocumentoAfiliado = null,
    int? PrestadorId = null,
    int? ContratanteId = null,
    int? IntermediarioId = null,
    int? TipoServicioId = null,
    EstadoAutorizacion? Estado = null,
    DateOnly? FechaDesde = null,
    DateOnly? FechaHasta = null,
    string? Cursor = null,
    int Limite = ListarAutorizacionesQuery.LimitePorDefecto) : IRequest<CursorPage<AutorizacionResumenDto>>
{
    public const int LimitePorDefecto = 20;
    public const int LimiteMaximo = 100;
}

/// <summary>Fila del listado: solo lo necesario para mostrar y filtrar, sin detalles.</summary>
public sealed record AutorizacionResumenDto(
    long Id,
    string Numero,
    EstadoAutorizacion Estado,
    DateOnly FechaServicio,
    int AfiliadoId,
    string NumeroAfiliado,
    string Afiliado,
    int PrestadorId,
    string Prestador,
    int ContratanteId,
    string Contratante,
    decimal MontoReclamado,
    decimal MontoArs,
    decimal MontoAfiliado,
    int CantidadServicios,
    DateTimeOffset CreadoEn);
