using HealthAuthorizations.Application.Common.Exceptions;
using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.Domain.Catalogos;
using HealthAuthorizations.Domain.Common;
using HealthAuthorizations.Infrastructure.Persistence.Seed;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    // Códigos de SQL Server para violación de índice único y de restricción UNIQUE.
    private const int SqlErrorUniqueIndex = 2601;
    private const int SqlErrorUniqueConstraint = 2627;

    public DbSet<Autorizacion> Autorizaciones => Set<Autorizacion>();

    public DbSet<Afiliado> Afiliados => Set<Afiliado>();

    public DbSet<Intermediario> Intermediarios => Set<Intermediario>();

    public DbSet<Prestador> Prestadores => Set<Prestador>();

    public DbSet<Contratante> Contratantes => Set<Contratante>();

    public DbSet<TipoPrestador> TiposPrestadores => Set<TipoPrestador>();

    public DbSet<TipoServicio> TiposServicios => Set<TipoServicio>();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("El recurso fue modificado por otra operación.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: SqlErrorUniqueIndex or SqlErrorUniqueConstraint } sql)
        {
            throw new DuplicateKeyException(sql.Message, ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        ConfigurarAuditoria(modelBuilder);
        CatalogosSeed.Aplicar(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Dinero: decimal(18,2) por convención en todo el modelo.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    private static void ConfigurarAuditoria(ModelBuilder modelBuilder)
    {
        var auditables = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(IAuditable).IsAssignableFrom(t.ClrType));

        foreach (var entityType in auditables)
        {
            var entity = modelBuilder.Entity(entityType.ClrType);
            entity.Property(nameof(IAuditable.CreadoPor)).HasMaxLength(AuditableEntity<int>.MaxLongitudUsuario).IsRequired();
            entity.Property(nameof(IAuditable.ModificadoPor)).HasMaxLength(AuditableEntity<int>.MaxLongitudUsuario);
        }
    }
}
