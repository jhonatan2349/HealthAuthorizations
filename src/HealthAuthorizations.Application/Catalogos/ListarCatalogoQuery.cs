using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Domain.Catalogos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Catalogos;

/// <summary>Catálogos simples (pocos registros, cambian poco): candidatos a caché en la Fase 7.</summary>
public enum TipoCatalogo
{
    TiposServicio,
    TiposPrestador,
    Contratantes,
    Intermediarios,
}

public sealed record ListarCatalogoQuery(TipoCatalogo Tipo, bool IncluirInactivos = false) : IRequest<IReadOnlyList<CatalogoDto>>;

internal sealed class ListarCatalogoHandler(IApplicationDbContext db) : IRequestHandler<ListarCatalogoQuery, IReadOnlyList<CatalogoDto>>
{
    public Task<IReadOnlyList<CatalogoDto>> Handle(ListarCatalogoQuery request, CancellationToken cancellationToken) =>
        request.Tipo switch
        {
            TipoCatalogo.TiposServicio => ListarAsync(db.TiposServicios, request.IncluirInactivos, cancellationToken),
            TipoCatalogo.TiposPrestador => ListarAsync(db.TiposPrestadores, request.IncluirInactivos, cancellationToken),
            TipoCatalogo.Contratantes => ListarAsync(db.Contratantes, request.IncluirInactivos, cancellationToken),
            TipoCatalogo.Intermediarios => ListarAsync(db.Intermediarios, request.IncluirInactivos, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Tipo, "Tipo de catálogo no soportado."),
        };

    private static async Task<IReadOnlyList<CatalogoDto>> ListarAsync<T>(IQueryable<T> set, bool incluirInactivos, CancellationToken cancellationToken)
        where T : Catalogo =>
        await set.AsNoTracking()
            .Where(c => incluirInactivos || c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new CatalogoDto(c.Id, c.Codigo, c.Nombre, c.Activo))
            .ToListAsync(cancellationToken);
}
