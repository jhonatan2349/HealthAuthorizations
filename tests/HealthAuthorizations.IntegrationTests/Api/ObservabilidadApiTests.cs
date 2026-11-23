using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using HealthAuthorizations.Application.Common.Telemetry;
using HealthAuthorizations.IntegrationTests.Infrastructure;
using static HealthAuthorizations.IntegrationTests.Api.ApiClientExtensions;

namespace HealthAuthorizations.IntegrationTests.Api;

public sealed class ObservabilidadApiTests(DatabaseFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Live_EsAnonimaYNoConsultaDependencias()
    {
        using var client = fixture.CreateAnonymousClient();

        using var response = await client.GetAsync("/health/live", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.JsonAsync();
        ((string)json["status"]!).ShouldBe("Healthy");
        json["checks"]!.AsObject().Count.ShouldBe(0);
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
    }

    [Fact]
    public async Task Ready_ConSqlServerDisponible_Devuelve200()
    {
        using var client = fixture.CreateAnonymousClient();

        using var response = await client.GetAsync("/health/ready", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ((string)(await response.JsonAsync())["checks"]!["sqlserver"]!["status"]!).ShouldBe("Healthy");
    }

    [Fact]
    public async Task Ready_SinSqlServer_Devuelve503YLiveSigueSano()
    {
        await using var api = fixture.CreateApi(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Server=127.0.0.1,1;Database=NoExiste;User Id=sa;Password=x;TrustServerCertificate=True;Connect Timeout=2",
        });
        using var client = api.CreateClient();

        using var ready = await client.GetAsync("/health/ready", Ct);
        using var live = await client.GetAsync("/health/live", Ct);
        var cuerpo = await ready.Content.ReadAsStringAsync(Ct);

        ready.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        cuerpo.ShouldContain("\"Unhealthy\"");
        cuerpo.ShouldNotContain("SqlException");
        live.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Sondas_NoEstanSujetasAlRateLimiting()
    {
        await using var api = fixture.CreateApi(new Dictionary<string, string?> { ["RateLimiting:PermisosPorMinuto"] = "1" });
        using var client = api.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            using var response = await client.GetAsync("/health/live", Ct);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task CorrelationId_EsElTraceIdDelSpanDelCasoDeUso()
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ApplicationTelemetry.Nombre,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => { lock (spans) { spans.Add(a); } },
        };
        ActivitySource.AddActivityListener(listener);
        using var client = fixture.CreateClient();

        using var response = await client.CrearAsync(CuerpoCrear());

        var correlationId = response.Headers.GetValues("X-Correlation-Id").Single();
        Activity[] delCaso;
        lock (spans)
        {
            delCaso = spans.Where(s => s.TraceId.ToString() == correlationId).ToArray();
        }

        delCaso.ShouldContain(s => s.DisplayName == "CrearAutorizacionCommand" && s.ParentSpanId != default);
    }

    [Fact]
    public async Task CrearPorHttp_RegistraLaMetricaDeNegocio()
    {
        long creadas = 0;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrumento, l) =>
            {
                if (instrumento.Name == "healthauth.autorizaciones.creadas")
                {
                    l.EnableMeasurementEvents(instrumento);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, valor, _, _) => Interlocked.Add(ref creadas, valor));
        listener.Start();
        using var client = fixture.CreateClient();

        using var response = await client.CrearAsync(CuerpoCrear());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        Interlocked.Read(ref creadas).ShouldBeGreaterThanOrEqualTo(1);
    }
}
