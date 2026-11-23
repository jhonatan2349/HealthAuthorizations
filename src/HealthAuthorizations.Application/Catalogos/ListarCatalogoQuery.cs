using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Domain.Catalogos;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Catalogos;

/// <summary>Catálogos simples: pocos registros que cambian poco.</summary>
public enum TipoCatalogo
{
    TiposServicio,
    TiposPrestador,
    Contratantes,
    Intermediarios,
}

public sealed record ListarCatalogoQuery(TipoCatalogo Tipo, bool IncluirInactivos = false) : IRequest<IReadOnlyList<CatalogoDto>>;

/// <summary>
/// Catálogos casi estáticos y muy consultados: se sirven desde caché (memoria local y, si está configurado,
/// Redis compartido entre réplicas). Los afiliados no se cachean: son datos personales y de alta cardinalidad.
/// </summary>
internal sealed class ListarCatalogoHandler(IApplicationDbContext db, HybridCache cache) : IRequestHandler<ListarCatalogoQuery, IReadOnlyList<CatalogoDto>>
{
    public const string EtiquetaCache = "catalogos";

    private static readonly HybridCacheEntryOptions OpcionesCache = new()
    {
        Expiration = TimeSpan.FromMinutes(10),

        // Más corto en memoria: las réplicas convergen pronto aunque solo se invalide Redis.
        LocalCacheExpiration = TimeSpan.FromMinutes(2),
    };

    public static string ClaveCache(TipoCatalogo tipo, bool incluirInactivos) =>
        $"catalogos:{tipo}:{(incluirInactivos ? "todos" : "activos")}";

    public async Task<IReadOnlyList<CatalogoDto>> Handle(ListarCatalogoQuery request, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            ClaveCache(request.Tipo, request.IncluirInactivos),
            request,
            (consulta, ct) => new ValueTask<List<CatalogoDto>>(ListarDesdeBaseDeDatosAsync(consulta, ct)),
            OpcionesCache,
            [EtiquetaCache],
            cancellationToken);

    private Task<List<CatalogoDto>> ListarDesdeBaseDeDatosAsync(ListarCatalogoQuery request, CancellationToken cancellationToken) =>
        request.Tipo switch
        {
            TipoCatalogo.TiposServicio => ListarAsync(db.TiposServicios, request.IncluirInactivos, cancellationToken),
            TipoCatalogo.TiposPrestador => ListarAsync(db.TiposPrestadores, request.IncluirInactivos, cancellationToken),
            TipoCatalogo.Contratantes => ListarAsync(db.Contratantes, request.IncluirInactivos, cancellationToken),
            TipoCatalogo.Intermediarios => ListarAsync(db.Intermediarios, request.IncluirInactivos, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Tipo, "Tipo de catálogo no soportado."),
        };

    private static Task<List<CatalogoDto>> ListarAsync<T>(IQueryable<T> set, bool incluirInactivos, CancellationToken cancellationToken)
        where T : Catalogo =>
        set.AsNoTracking()
            .Where(c => incluirInactivos || c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new CatalogoDto(c.Id, c.Codigo, c.Nombre, c.Activo))
            .ToListAsync(cancellationToken);
}
