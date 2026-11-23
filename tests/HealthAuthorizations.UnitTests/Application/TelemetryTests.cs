using System.Diagnostics;
using System.Diagnostics.Metrics;
using HealthAuthorizations.Application.Common.Behaviors;
using HealthAuthorizations.Application.Common.Results;
using HealthAuthorizations.Application.Common.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace HealthAuthorizations.UnitTests.Application;

public sealed class TelemetryTests
{
    public sealed record SolicitudDePrueba;

    private static List<Activity> Escuchar(out ActivityListener listener)
    {
        var actividades = new List<Activity>();
        listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ApplicationTelemetry.Nombre,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => { lock (actividades) { actividades.Add(a); } },
        };
        ActivitySource.AddActivityListener(listener);
        return actividades;
    }

    [Fact]
    public async Task Tracing_CreaUnSpanPorCasoDeUsoConElNombreDeLaSolicitud()
    {
        var actividades = Escuchar(out var listener);
        using var _ = listener;

        await new TracingBehavior<SolicitudDePrueba, Result>().Handle(new SolicitudDePrueba(), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        var span = actividades.ShouldHaveSingleItem();
        span.DisplayName.ShouldBe(nameof(SolicitudDePrueba));
        span.Status.ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task Tracing_ConResultadoFallido_MarcaErrorConElCodigo()
    {
        var actividades = Escuchar(out var listener);
        using var _ = listener;

        await new TracingBehavior<SolicitudDePrueba, Result>().Handle(
            new SolicitudDePrueba(), _ => Task.FromResult(Result.Failure(Error.NotFound("Prueba.NoExiste", "x"))), CancellationToken.None);

        var span = actividades.ShouldHaveSingleItem();
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.GetTagItem(TracingBehavior<SolicitudDePrueba, Result>.EtiquetaCodigoError).ShouldBe("Prueba.NoExiste");
    }

    [Fact]
    public async Task Tracing_ConExcepcion_MarcaErrorYRelanza()
    {
        var actividades = Escuchar(out var listener);
        using var _ = listener;

        await Should.ThrowAsync<InvalidOperationException>(() => new TracingBehavior<SolicitudDePrueba, Result>().Handle(
            new SolicitudDePrueba(), _ => throw new InvalidOperationException("falla"), CancellationToken.None));

        var span = actividades.ShouldHaveSingleItem();
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.Events.ShouldContain(e => e.Name == "exception");
    }

    [Fact]
    public void Metricas_RegistranContadoresYMontoReclamado()
    {
        using var provider = new ServiceCollection().AddMetrics().AddSingleton<ApplicationTelemetry>().BuildServiceProvider();
        var telemetry = provider.GetRequiredService<ApplicationTelemetry>();
        var mediciones = new Dictionary<string, double>();

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrumento, l) =>
            {
                if (instrumento.Meter.Name == ApplicationTelemetry.Nombre)
                {
                    l.EnableMeasurementEvents(instrumento);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((i, valor, _, _) => Sumar(mediciones, i.Name, valor));
        listener.SetMeasurementEventCallback<double>((i, valor, _, _) => Sumar(mediciones, i.Name, valor));
        listener.Start();

        telemetry.AutorizacionCreada(1500m);
        telemetry.AutorizacionCreada(500m);
        telemetry.ReintentoIdempotente();
        telemetry.AutorizacionActualizada();
        telemetry.AutorizacionInactivada();

        mediciones["healthauth.autorizaciones.creadas"].ShouldBe(2);
        mediciones["healthauth.autorizaciones.monto_reclamado"].ShouldBe(2000);
        mediciones["healthauth.autorizaciones.reintentos_idempotentes"].ShouldBe(1);
        mediciones["healthauth.autorizaciones.actualizadas"].ShouldBe(1);
        mediciones["healthauth.autorizaciones.inactivadas"].ShouldBe(1);
    }

    private static void Sumar(Dictionary<string, double> mediciones, string nombre, double valor)
    {
        lock (mediciones)
        {
            mediciones[nombre] = mediciones.GetValueOrDefault(nombre) + valor;
        }
    }
}
