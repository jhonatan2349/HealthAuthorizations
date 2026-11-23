using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthAuthorizations.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Completa los campos de auditoría de forma transversal, sin que los casos de uso tengan que recordarlo.
/// También marca como modificado el agregado cuando solo cambian sus hijos, lo que además
/// actualiza su rowversion y mantiene correcta la concurrencia optimista sobre el agregado completo.
/// </summary>
public sealed class AuditableEntityInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    internal const string UsuarioSistema = "sistema";

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Auditar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Auditar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Auditar(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var ahora = timeProvider.GetUtcNow();
        var usuario = string.IsNullOrWhiteSpace(currentUser.UsuarioId) ? UsuarioSistema : currentUser.UsuarioId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IAuditable.CreadoEn)).CurrentValue = ahora;
                entry.Property(nameof(IAuditable.CreadoPor)).CurrentValue = usuario;
            }
            else if (entry.State == EntityState.Modified || TieneHijosModificados(entry))
            {
                entry.Property(nameof(IAuditable.ModificadoEn)).CurrentValue = ahora;
                entry.Property(nameof(IAuditable.ModificadoPor)).CurrentValue = usuario;
            }
        }
    }

    private static bool TieneHijosModificados(EntityEntry entry) =>
        entry.Collections
            .Where(c => c.CurrentValue is not null)
            .SelectMany(c => c.CurrentValue!.Cast<object>())
            .Select(hijo => entry.Context.Entry(hijo).State)
            .Any(estado => estado is EntityState.Added or EntityState.Modified or EntityState.Deleted);
}
