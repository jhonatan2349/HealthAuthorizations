using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using HealthAuthorizations.Api.Security;
using HealthAuthorizations.Application.Autorizaciones.Actualizar;
using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Autorizaciones.Inactivar;
using HealthAuthorizations.Application.Common.Exceptions;
using HealthAuthorizations.Application.IntegrationEvents;
using HealthAuthorizations.Infrastructure.Messaging;
using HealthAuthorizations.Infrastructure.Persistence;
using HealthAuthorizations.Infrastructure.Persistence.Outbox;
using HealthAuthorizations.IntegrationTests.Autorizaciones;
using HealthAuthorizations.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using static HealthAuthorizations.IntegrationTests.Api.ApiClientExtensions;

namespace HealthAuthorizations.IntegrationTests.Messaging;

/// <summary>Las pruebas de esta clase corren en secuencia: comparten el outbox y procesan lotes.</summary>
public sealed class OutboxTests(DatabaseFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<List<OutboxMessage>> MensajesDeAsync(long autorizacionId) =>
        fixture.QueryAsync(db => db.OutboxMessages.AsNoTracking()
            .Where(m => m.Contenido.Contains($"\"autorizacionId\":{autorizacionId},"))
            .OrderBy(m => m.OcurridoEn)
            .ToListAsync(Ct));

    private async Task<long> CrearAsync() => (await fixture.SendAsync(Datos.Crear())).Value.Autorizacion.Id;

    [Fact]
    public async Task Crear_GuardaElEventoEnElOutboxConElNumeroDeLaBaseDeDatos()
    {
        var resultado = (await fixture.SendAsync(Datos.Crear())).Value.Autorizacion;

        var mensaje = (await MensajesDeAsync(resultado.Id)).ShouldHaveSingleItem();
        mensaje.Tipo.ShouldBe(AutorizacionCreadaV1.NombreTipo);
        mensaje.ProcesadoEn.ShouldBeNull();

        var cuerpo = JsonNode.Parse(mensaje.Contenido)!;
        ((string)cuerpo["numero"]!).ShouldBe(resultado.Numero);
        ((decimal)cuerpo["montoArs"]!).ShouldBe(resultado.MontoArs);
        cuerpo["diagnostico"].ShouldBeNull();
    }

    [Fact]
    public async Task ActualizarEInactivar_AgreganSusEventosEnOrden()
    {
        var id = await CrearAsync();
        await fixture.SendAsync(new ActualizarAutorizacionCommand(id, 7, Datos.Hoy, null, null, [new ServicioRequest(5, "Rayos X", 900m)]));
        await fixture.SendAsync(new InactivarAutorizacionCommand(id, "Prueba"));

        var tipos = (await MensajesDeAsync(id)).Select(m => m.Tipo);

        tipos.ShouldBe([AutorizacionCreadaV1.NombreTipo, AutorizacionActualizadaV1.NombreTipo, AutorizacionEstadoCambiadoV1.NombreTipo]);
    }

    [Fact]
    public async Task OperacionFallida_NoDejaEventosEnElOutbox()
    {
        var original = (await fixture.SendAsync(Datos.Crear())).Value.Autorizacion;
        var cambio = new ActualizarAutorizacionCommand(original.Id, 7, Datos.Hoy, null, null, [new ServicioRequest(5, "Rayos X", 900m)], original.Version);
        await fixture.SendAsync(cambio);

        await Should.ThrowAsync<ConcurrencyConflictException>(() => fixture.SendAsync(cambio));

        (await MensajesDeAsync(original.Id)).Count(m => m.Tipo == AutorizacionActualizadaV1.NombreTipo).ShouldBe(1);
    }

    [Fact]
    public async Task Dispatcher_PublicaYMarcaComoProcesado()
    {
        var id = await CrearAsync();
        var publicador = new PublicadorFalso();

        await ProcesarHastaVaciarAsync(publicador);

        var mensaje = (await MensajesDeAsync(id)).ShouldHaveSingleItem();
        mensaje.ProcesadoEn.ShouldNotBeNull();
        publicador.Publicados.ShouldContain(m => m.MessageId == mensaje.Id && m.Tipo == AutorizacionCreadaV1.NombreTipo);
    }

    [Fact]
    public async Task Dispatcher_SiElBrokerFalla_ReintentaConBackoffSinPerderElMensaje()
    {
        var id = await CrearAsync();

        await ProcesarLoteAsync(new PublicadorFalso(fallar: true));

        var fallido = (await MensajesDeAsync(id)).ShouldHaveSingleItem();
        fallido.ProcesadoEn.ShouldBeNull();
        fallido.Intentos.ShouldBe(1);
        fallido.UltimoError!.ShouldContain("broker caído");
        fallido.BloqueadoHasta!.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow);

        // Durante el backoff ninguna réplica lo vuelve a tomar.
        var sano = new PublicadorFalso();
        await ProcesarLoteAsync(sano);
        sano.Publicados.ShouldNotContain(m => m.MessageId == fallido.Id);
    }

    [Fact]
    public async Task DosReplicasEnParalelo_NoPublicanElMismoMensajeDosVeces()
    {
        var ids = new List<long>();
        for (var i = 0; i < 12; i++)
        {
            ids.Add(await CrearAsync());
        }

        var publicador = new PublicadorFalso(demora: TimeSpan.FromMilliseconds(20));
        await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => ProcesarHastaVaciarAsync(publicador, tamanoLote: 3)));

        var nuestros = new HashSet<Guid>();
        foreach (var id in ids)
        {
            nuestros.Add((await MensajesDeAsync(id)).Single().Id);
        }

        var publicadosNuestros = publicador.Publicados.Where(m => nuestros.Contains(m.MessageId)).ToList();
        publicadosNuestros.Count.ShouldBe(12);
        publicadosNuestros.Select(m => m.MessageId).Distinct().Count().ShouldBe(12);
    }

    [Fact]
    public async Task ConRabbitMqReal_ElEventoLlegaALaColaDeUnConsumidor()
    {
        var rabbit = fixture.RabbitMqUri;
        var credenciales = rabbit.UserInfo.Split(':');
        const string exchange = "healthauthorizations.eventos";

        var factory = new ConnectionFactory { Uri = rabbit };
        await using var conexion = await factory.CreateConnectionAsync(Ct);
        await using var canal = await conexion.CreateChannelAsync(cancellationToken: Ct);
        await canal.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: Ct);
        var cola = (await canal.QueueDeclareAsync($"prueba-{Guid.NewGuid():N}", durable: false, exclusive: true, autoDelete: true, cancellationToken: Ct)).QueueName;
        await canal.QueueBindAsync(cola, exchange, "autorizacion.#", cancellationToken: Ct);

        await using var api = fixture.CreateApi(new Dictionary<string, string?>
        {
            ["Mensajeria:Habilitada"] = "true",
            ["Mensajeria:RabbitMq:Host"] = rabbit.Host,
            ["Mensajeria:RabbitMq:Puerto"] = rabbit.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Mensajeria:RabbitMq:Usuario"] = credenciales[0],
            ["Mensajeria:RabbitMq:Clave"] = credenciales[1],
            ["Mensajeria:Outbox:IntervaloSegundos"] = "1",
        });
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", api.Services.GetRequiredService<JwtTokenIssuer>().Emitir("rabbit", "R", [Roles.Operador]).AccessToken);

        using var creada = await client.CrearAsync(CuerpoCrear());
        var numero = (string)(await creada.JsonAsync())["numero"]!;
        var recibido = await EsperarMensajeAsync(canal, cola, numero);

        recibido.BasicProperties.Type.ShouldBe(AutorizacionCreadaV1.NombreTipo);
        recibido.RoutingKey.ShouldBe(AutorizacionCreadaV1.NombreTipo);
        recibido.BasicProperties.ContentType.ShouldBe("application/json");
        recibido.BasicProperties.DeliveryMode.ShouldBe(DeliveryModes.Persistent);
        Guid.TryParse(recibido.BasicProperties.MessageId, out _).ShouldBeTrue();
        recibido.BasicProperties.Headers!.ShouldContainKey("traceparent");

        using var ready = await client.GetAsync("/health/ready", Ct);
        ((string)(await ready.JsonAsync())["checks"]!["rabbitmq"]!["status"]!).ShouldBe("Healthy");
    }

    [Fact]
    public async Task SinBroker_ReadinessQuedaDegradadoPeroDisponible()
    {
        await using var api = fixture.CreateApi(new Dictionary<string, string?>
        {
            ["Mensajeria:Habilitada"] = "true",
            ["Mensajeria:RabbitMq:Host"] = "127.0.0.1",
            ["Mensajeria:RabbitMq:Puerto"] = "1",
            ["Mensajeria:RabbitMq:Usuario"] = "x",
            ["Mensajeria:RabbitMq:Clave"] = "x",
            ["Mensajeria:Outbox:IntervaloSegundos"] = "3600",
        });
        using var client = api.CreateClient();

        using var ready = await client.GetAsync("/health/ready", Ct);

        ready.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await ready.JsonAsync();
        ((string)json["status"]!).ShouldBe("Degraded");
        ((string)json["checks"]!["rabbitmq"]!["status"]!).ShouldBe("Degraded");
        ((string)json["checks"]!["sqlserver"]!["status"]!).ShouldBe("Healthy");
    }

    private static async Task<BasicGetResult> EsperarMensajeAsync(IChannel canal, string cola, string numero)
    {
        var limite = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < limite)
        {
            if (await canal.BasicGetAsync(cola, autoAck: true, Ct) is { } mensaje
                && Encoding.UTF8.GetString(mensaje.Body.Span).Contains(numero, StringComparison.Ordinal))
            {
                return mensaje;
            }

            await Task.Delay(250, Ct);
        }

        throw new TimeoutException($"No llegó el evento de {numero} a la cola.");
    }

    private async Task<int> ProcesarLoteAsync(IMessagePublisher publicador, int tamanoLote = 500)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var opciones = Options.Create(new MensajeriaOptions
        {
            Habilitada = true,
            RabbitMq = new RabbitMqOptions { Usuario = "x", Clave = "x" },
            Outbox = new OutboxOptions { TamanoLote = tamanoLote, MaxIntentos = 5, LeaseSegundos = 60 },
        });
        var dispatcher = ActivatorUtilities.CreateInstance<OutboxDispatcher>(
            scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), publicador, opciones);
        return await dispatcher.ProcesarLoteAsync(Ct);
    }

    private async Task ProcesarHastaVaciarAsync(IMessagePublisher publicador, int tamanoLote = 500)
    {
        while (await ProcesarLoteAsync(publicador, tamanoLote) > 0)
        {
        }
    }

    private sealed class PublicadorFalso(bool fallar = false, TimeSpan demora = default) : IMessagePublisher
    {
        private readonly ConcurrentBag<MensajeSaliente> _publicados = [];

        public IReadOnlyCollection<MensajeSaliente> Publicados => _publicados;

        public async Task PublicarAsync(MensajeSaliente mensaje, CancellationToken cancellationToken)
        {
            if (demora > TimeSpan.Zero)
            {
                await Task.Delay(demora, cancellationToken);
            }

            if (fallar)
            {
                throw new InvalidOperationException("broker caído");
            }

            _publicados.Add(mensaje);
        }
    }
}
