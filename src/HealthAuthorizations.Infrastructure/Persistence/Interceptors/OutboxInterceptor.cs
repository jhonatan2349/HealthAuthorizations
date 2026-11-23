using System.Diagnostics;
using System.Text.Json;
using HealthAuthorizations.Application.IntegrationEvents;
using HealthAuthorizations.Domain.Common;
using HealthAuthorizations.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HealthAuthorizations.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Convierte los eventos de dominio de los agregados modificados en mensajes del outbox,
/// dentro del mismo SaveChanges (misma transacción). Los casos de uso no saben que existe mensajería.
/// </summary>
public sealed class OutboxInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AgregarMensajes(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AgregarMensajes(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AgregarMensajes(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var agregados = context.ChangeTracker.Entries()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        if (agregados.Count == 0)
        {
            return;
        }

        var ahora = timeProvider.GetUtcNow();
        var traceParent = Activity.Current?.Id;

        foreach (var agregado in agregados)
        {
            foreach (var evento in agregado.DomainEvents)
            {
                if (IntegrationEventMapper.Map(evento) is not { } integracion)
                {
                    continue;
                }

                context.Set<OutboxMessage>().Add(new OutboxMessage
                {
                    Id = Guid.CreateVersion7(ahora),
                    Tipo = integracion.Tipo,
                    Contenido = JsonSerializer.Serialize(integracion, integracion.GetType(), Json),
                    OcurridoEn = ahora,
                    TraceParent = traceParent is { Length: <= OutboxMessage.MaxLongitudTraceParent } ? traceParent : null,
                });
            }

            agregado.ClearDomainEvents();
        }
    }
}
