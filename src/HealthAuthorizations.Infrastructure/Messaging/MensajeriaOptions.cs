namespace HealthAuthorizations.Infrastructure.Messaging;

/// <summary>Configuración de mensajería (sección "Mensajeria").</summary>
public sealed class MensajeriaOptions
{
    public const string SectionName = "Mensajeria";

    /// <summary>
    /// Si es false, los eventos se siguen guardando en el outbox (consistencia) pero no se publican;
    /// al habilitarla, el procesador publica los pendientes.
    /// </summary>
    public bool Habilitada { get; init; }

    public RabbitMqOptions RabbitMq { get; init; } = new();

    public OutboxOptions Outbox { get; init; } = new();

    public bool EsValida =>
        !Habilitada
        || (!string.IsNullOrWhiteSpace(RabbitMq.Host)
            && !string.IsNullOrWhiteSpace(RabbitMq.Usuario)
            && !string.IsNullOrWhiteSpace(RabbitMq.Clave)
            && !string.IsNullOrWhiteSpace(RabbitMq.Exchange)
            && Outbox.TamanoLote is > 0 and <= 500
            && Outbox.MaxIntentos > 0
            && Outbox.IntervaloSegundos > 0);
}

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";

    public int Puerto { get; init; } = 5672;

    public string VirtualHost { get; init; } = "/";

    public string Usuario { get; init; } = string.Empty;

    /// <summary>Secreto: user-secrets, variable de entorno o K8s Secret.</summary>
    public string Clave { get; init; } = string.Empty;

    /// <summary>Exchange topic donde se publican los eventos; la routing key es el tipo del evento.</summary>
    public string Exchange { get; init; } = "healthauthorizations.eventos";
}

public sealed class OutboxOptions
{
    public int IntervaloSegundos { get; init; } = 5;

    public int TamanoLote { get; init; } = 50;

    /// <summary>Al alcanzarlo, el mensaje deja de reintentarse y queda para revisión manual.</summary>
    public int MaxIntentos { get; init; } = 10;

    /// <summary>Tiempo durante el cual una réplica se reserva un lote antes de que otra pueda tomarlo.</summary>
    public int LeaseSegundos { get; init; } = 60;
}
