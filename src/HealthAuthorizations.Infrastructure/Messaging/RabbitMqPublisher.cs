using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace HealthAuthorizations.Infrastructure.Messaging;

/// <summary>
/// Publica en un exchange topic de RabbitMQ con mensajes persistentes y publisher confirms:
/// <see cref="PublicarAsync"/> solo termina cuando el broker confirmó que guardó el mensaje.
/// La conexión es única por proceso (se recupera sola ante caídas) y se crea al primer uso.
/// </summary>
public sealed class RabbitMqPublisher(IOptions<MensajeriaOptions> options) : IMessagePublisher, IAsyncDisposable
{
    private const string ContentType = "application/json";

    private readonly RabbitMqOptions _rabbit = options.Value.RabbitMq;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublicarAsync(MensajeSaliente mensaje, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mensaje);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var channel = await ObtenerCanalAsync(cancellationToken);
            var propiedades = new BasicProperties
            {
                MessageId = mensaje.MessageId.ToString(),
                Type = mensaje.Tipo,
                ContentType = ContentType,
                DeliveryMode = DeliveryModes.Persistent,
                Timestamp = new AmqpTimestamp(mensaje.OcurridoEn.ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?>(),
            };

            // Propagación W3C: el consumidor continúa la misma traza.
            if (Activity.Current?.Id is { } traceParent)
            {
                propiedades.Headers["traceparent"] = traceParent;
            }

            await channel.BasicPublishAsync(
                _rabbit.Exchange,
                routingKey: mensaje.Tipo,
                mandatory: false,
                basicProperties: propiedades,
                body: Encoding.UTF8.GetBytes(mensaje.Contenido),
                cancellationToken: cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Verifica (o establece) la conexión con el broker; usado por el health check.</summary>
    public async Task<bool> EstaDisponibleAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return (await ObtenerCanalAsync(cancellationToken)).IsOpen;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }

    private async Task<IChannel> ObtenerCanalAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        if (_connection is not { IsOpen: true })
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbit.Host,
                Port = _rabbit.Puerto,
                VirtualHost = _rabbit.VirtualHost,
                UserName = _rabbit.Usuario,
                Password = _rabbit.Clave,
                AutomaticRecoveryEnabled = true,
                ClientProvidedName = "healthauthorizations-api",
            };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
        }

        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);

        await _channel.ExchangeDeclareAsync(_rabbit.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);
        return _channel;
    }
}
