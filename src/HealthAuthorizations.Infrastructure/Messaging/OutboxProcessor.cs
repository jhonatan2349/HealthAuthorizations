using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthAuthorizations.Infrastructure.Messaging;

/// <summary>
/// Proceso en segundo plano que vacía el outbox periódicamente. Cada réplica ejecuta el suyo;
/// el lease evita publicaciones duplicadas entre réplicas. Un fallo nunca detiene el proceso.
/// </summary>
public sealed partial class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<MensajeriaOptions> options,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var outbox = options.Value.Outbox;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(outbox.IntervaloSegundos));

        do
        {
            try
            {
                int publicados;
                do
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    publicados = await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().ProcesarLoteAsync(stoppingToken);
                }
                while (publicados == outbox.TamanoLote && !stoppingToken.IsCancellationRequested); // hay más pendientes
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Base de datos no disponible u otro fallo: se registra y se reintenta en el próximo ciclo.
                LogErrorCiclo(ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error al procesar el outbox; se reintentará en el próximo ciclo")]
    private partial void LogErrorCiclo(Exception exception);
}
