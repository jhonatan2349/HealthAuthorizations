namespace HealthAuthorizations.Infrastructure.Persistence.Outbox;

/// <summary>
/// Evento de integración pendiente de publicar. Se inserta en la misma transacción que el cambio
/// que lo origina (Transactional Outbox): no hay eventos perdidos si cae el broker ni eventos
/// fantasma si falla la base de datos.
/// </summary>
public sealed class OutboxMessage
{
    public const int MaxLongitudTipo = 150;
    public const int MaxLongitudTraceParent = 55;
    public const int MaxLongitudError = 2000;

    /// <summary>Identificador estable del mensaje: los consumidores lo usan para descartar duplicados.</summary>
    public Guid Id { get; init; }

    public string Tipo { get; init; } = string.Empty;

    public string Contenido { get; init; } = string.Empty;

    public DateTimeOffset OcurridoEn { get; init; }

    /// <summary>Contexto W3C de la traza que originó el evento, para continuar la traza al publicar.</summary>
    public string? TraceParent { get; init; }

    public DateTimeOffset? ProcesadoEn { get; set; }

    public int Intentos { get; set; }

    public string? UltimoError { get; set; }

    /// <summary>Lease: mientras no venza, ninguna otra réplica toma el mensaje. También aplica el backoff entre reintentos.</summary>
    public DateTimeOffset? BloqueadoHasta { get; set; }
}
