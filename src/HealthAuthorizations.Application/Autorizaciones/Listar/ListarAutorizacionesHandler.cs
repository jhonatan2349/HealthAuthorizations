using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Application.Common.Pagination;
using HealthAuthorizations.Domain.Autorizaciones;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Autorizaciones.Listar;

/// <summary>
/// Paginación keyset: <c>WHERE Id &lt; @cursor ORDER BY Id DESC</c> usa el índice agrupado (o la clave incluida
/// en cada índice no agrupado) y cuesta lo mismo en la página 1 que en la 10.000, a diferencia de OFFSET.
/// Se pide una fila de más para saber si hay otra página sin ejecutar COUNT(*).
/// </summary>
internal sealed class ListarAutorizacionesHandler(IApplicationDbContext db)
    : IRequestHandler<ListarAutorizacionesQuery, CursorPage<AutorizacionResumenDto>>
{
    public async Task<CursorPage<AutorizacionResumenDto>> Handle(ListarAutorizacionesQuery request, CancellationToken cancellationToken)
    {
        var pagina = Filtrar(db.Autorizaciones.AsNoTracking(), request)
            .OrderByDescending(a => a.Id)
            .Take(request.Limite + 1);

        var filas = await (
            from a in pagina
            join af in db.Afiliados on a.AfiliadoId equals af.Id
            join p in db.Prestadores on a.PrestadorId equals p.Id
            join c in db.Contratantes on a.ContratanteId equals c.Id
            orderby a.Id descending
            select new AutorizacionResumenDto(
                a.Id,
                a.Numero,
                a.Estado,
                a.FechaServicio,
                af.Id,
                af.NumeroAfiliado,
                af.Nombres + " " + af.Apellidos,
                p.Id,
                p.Nombre,
                c.Id,
                c.Nombre,
                a.MontoReclamado,
                a.MontoArs,
                a.MontoAfiliado,
                a.Detalles.Count,
                a.CreadoEn))
            .ToListAsync(cancellationToken);

        var hayMas = filas.Count > request.Limite;
        var items = hayMas ? filas.Take(request.Limite).ToList() : filas;
        var siguiente = hayMas ? Cursor.Encode(items[^1].Id) : null;

        return new CursorPage<AutorizacionResumenDto>(items, siguiente, request.Limite);
    }

    private IQueryable<Autorizacion> Filtrar(IQueryable<Autorizacion> query, ListarAutorizacionesQuery f)
    {
        if (Cursor.TryDecode(f.Cursor, out var ultimoId))
        {
            query = query.Where(a => a.Id < ultimoId);
        }

        if (!string.IsNullOrWhiteSpace(f.Numero))
        {
            var numero = f.Numero.Trim().ToUpperInvariant();
            query = query.Where(a => a.Numero == numero);
        }

        if (!string.IsNullOrWhiteSpace(f.DocumentoAfiliado))
        {
            var documento = f.DocumentoAfiliado.Trim();
            query = query.Where(a => db.Afiliados.Any(af => af.Id == a.AfiliadoId && af.Documento == documento));
        }

        if (f.TipoServicioId is { } tipoServicioId)
        {
            query = query.Where(a => a.Detalles.Any(d => d.TipoServicioId == tipoServicioId));
        }

        if (f.AfiliadoId is { } afiliadoId)
        {
            query = query.Where(a => a.AfiliadoId == afiliadoId);
        }

        if (f.PrestadorId is { } prestadorId)
        {
            query = query.Where(a => a.PrestadorId == prestadorId);
        }

        if (f.ContratanteId is { } contratanteId)
        {
            query = query.Where(a => a.ContratanteId == contratanteId);
        }

        if (f.IntermediarioId is { } intermediarioId)
        {
            query = query.Where(a => a.IntermediarioId == intermediarioId);
        }

        if (f.Estado is { } estado)
        {
            query = query.Where(a => a.Estado == estado);
        }

        if (f.FechaDesde is { } desde)
        {
            query = query.Where(a => a.FechaServicio >= desde);
        }

        if (f.FechaHasta is { } hasta)
        {
            query = query.Where(a => a.FechaServicio <= hasta);
        }

        return query;
    }
}
