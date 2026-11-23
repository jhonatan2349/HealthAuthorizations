using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthAuthorizations.Infrastructure.Messaging;

/// <summary>
/// Estado del broker. Se registra como Degraded (no Unhealthy): si RabbitMQ cae la API sigue atendiendo
/// y los eventos se acumulan en el outbox; sacar el pod del balanceo no ayudaría.
/// </summary>
internal sealed class RabbitMqHealthCheck(RabbitMqPublisher publisher) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await publisher.EstaDisponibleAsync(cancellationToken)
                ? HealthCheckResult.Healthy("RabbitMQ disponible.")
                : new HealthCheckResult(context.Registration.FailureStatus, "RabbitMQ no disponible; los eventos se acumulan en el outbox.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "RabbitMQ no disponible; los eventos se acumulan en el outbox.", ex);
        }
    }
}
