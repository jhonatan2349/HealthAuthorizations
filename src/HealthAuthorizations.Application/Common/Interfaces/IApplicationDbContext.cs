using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.Domain.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HealthAuthorizations.Application.Common.Interfaces;

/// <summary>
/// Puerto de persistencia para los casos de uso. EF Core ya implementa Repository y Unit of Work,
/// por lo que se expone el contexto en lugar de envolverlo en repositorios genéricos (ver ADR-005).
/// Los detalles de una autorización se acceden siempre a través de su agregado.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Autorizacion> Autorizaciones { get; }

    DbSet<Afiliado> Afiliados { get; }

    DbSet<Intermediario> Intermediarios { get; }

    DbSet<Prestador> Prestadores { get; }

    DbSet<Contratante> Contratantes { get; }

    DbSet<TipoPrestador> TiposPrestadores { get; }

    DbSet<TipoServicio> TiposServicios { get; }

    /// <summary>Acceso al seguimiento de cambios; se usa para fijar la versión esperada (If-Match) en la concurrencia optimista.</summary>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;

    /// <summary>
    /// Persiste los cambios en una transacción.
    /// Lanza <c>ConcurrencyConflictException</c> o <c>DuplicateKeyException</c> en lugar de excepciones del proveedor.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
