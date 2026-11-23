using System.Diagnostics;
using HealthAuthorizations.Infrastructure.Persistence;
using HealthAuthorizations.Infrastructure.Persistence.Outbox;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthAuthorizations.Infrastructure.Messaging;

/// <summary>
/// Publica un lote de mensajes pendientes. Entrega "al menos una vez": si el proceso cae entre
/// publicar y marcar como procesado, el mensaje se vuelve a publicar con el mismo MessageId.
/// </summary>
public sealed partial class OutboxDispatcher(
    ApplicationDbContext db,
    IMessagePublisher publisher,
    IOptions<MensajeriaOptions> options,
    TimeProvider timeProvider,
    MensajeriaTelemetry telemetry,
    ILogger<OutboxDispatcher> logger)
{
    private static readonly TimeSpan BackoffMaximo = TimeSpan.FromMinutes(5);

    // Lease atómico: toma hasta N pendientes en orden y los reserva por un tiempo.
    // READPAST salta las filas que otra réplica ya bloqueó, así dos instancias nunca toman el mismo mensaje.
    private const string SqlReservarLote = """
        WITH lote AS (
            SELECT TOP (@lote) *
            FROM [OutboxMessages] WITH (ROWLOCK, UPDLOCK, READPAST)
            WHERE [ProcesadoEn] IS NULL
              AND [Intentos] < @maxIntentos
              AND ([BloqueadoHasta] IS NULL OR [BloqueadoHasta] <= @ahora)
            ORDER BY [OcurridoEn])
        UPDATE lote SET [BloqueadoHasta] = @hasta
        OUTPUT inserted.[Id], inserted.[Tipo], inserted.[Contenido], inserted.[OcurridoEn], inserted.[TraceParent], inserted.[Intentos];
        """;

    private readonly OutboxOptions _outbox = options.Value.Outbox;

    /// <summary>Procesa un lote y devuelve cuántos mensajes se publicaron.</summary>
    public async Task<int> ProcesarLoteAsync(CancellationToken cancellationToken)
    {
        var ahora = timeProvider.GetUtcNow();

        var lote = await db.Database.SqlQueryRaw<MensajeReservado>(
                SqlReservarLote,
                new SqlParameter("@lote", _outbox.TamanoLote),
                new SqlParameter("@maxIntentos", _outbox.MaxIntentos),
                new SqlParameter("@ahora", ahora),
                new SqlParameter("@hasta", ahora.AddSeconds(_outbox.LeaseSegundos)))
            .ToListAsync(cancellationToken);

        var publicados = 0;
        foreach (var mensaje in lote.OrderBy(m => m.OcurridoEn))
        {
            if (await PublicarAsync(mensaje, cancellationToken))
            {
                publicados++;
            }
        }

        return publicados;
    }

    private async Task<bool> PublicarAsync(MensajeReservado mensaje, CancellationToken cancellationToken)
    {
        ActivityContext.TryParse(mensaje.TraceParent, null, out var origen);
        using var activity = MensajeriaTelemetry.ActivitySource.StartActivity($"{mensaje.Tipo} publish", ActivityKind.Producer, origen);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", options.Value.RabbitMq.Exchange);
        activity?.SetTag("messaging.message.id", mensaje.Id.ToString());

        try
        {
            await publisher.PublicarAsync(new MensajeSaliente(mensaje.Id, mensaje.Tipo, mensaje.Contenido, mensaje.OcurridoEn), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            await RegistrarFalloAsync(mensaje, ex, cancellationToken);
            return false;
        }

        await db.OutboxMessages
            .Where(m => m.Id == mensaje.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.ProcesadoEn, timeProvider.GetUtcNow())
                .SetProperty(m => m.BloqueadoHasta, (DateTimeOffset?)null)
                .SetProperty(m => m.UltimoError, (string?)null), cancellationToken);

        telemetry.Publicado(mensaje.Tipo);
        return true;
    }

    private async Task RegistrarFalloAsync(MensajeReservado mensaje, Exception ex, CancellationToken cancellationToken)
    {
        var intentos = mensaje.Intentos + 1;
        var error = $"{ex.GetType().Name}: {ex.Message}";
        if (error.Length > OutboxMessage.MaxLongitudError)
        {
            error = error[..OutboxMessage.MaxLongitudError];
        }

        // Backoff exponencial usando el propio lease: 2, 4, 8... segundos, hasta 5 minutos.
        var espera = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, intentos), BackoffMaximo.TotalSeconds));

        await db.OutboxMessages
            .Where(m => m.Id == mensaje.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Intentos, intentos)
                .SetProperty(m => m.UltimoError, error)
                .SetProperty(m => m.BloqueadoHasta, timeProvider.GetUtcNow().Add(espera)), cancellationToken);

        telemetry.Fallido(mensaje.Tipo);

        if (intentos >= _outbox.MaxIntentos)
        {
            telemetry.Agotado(mensaje.Tipo);
            LogAgotado(ex, mensaje.Id, mensaje.Tipo, intentos);
        }
        else
        {
            LogReintento(mensaje.Id, mensaje.Tipo, intentos, (int)espera.TotalSeconds, ex.GetType().Name);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo publicar {MessageId} ({Tipo}); intento {Intentos}, nuevo intento en {Segundos} s: {Error}")]
    private partial void LogReintento(Guid messageId, string tipo, int intentos, int segundos, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "El mensaje {MessageId} ({Tipo}) agotó {Intentos} intentos y requiere revisión manual")]
    private partial void LogAgotado(Exception exception, Guid messageId, string tipo, int intentos);
}

/// <summary>Proyección de las columnas devueltas por el lease.</summary>
public sealed class MensajeReservado
{
    public Guid Id { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Contenido { get; set; } = string.Empty;

    public DateTimeOffset OcurridoEn { get; set; }

    public string? TraceParent { get; set; }

    public int Intentos { get; set; }
}
