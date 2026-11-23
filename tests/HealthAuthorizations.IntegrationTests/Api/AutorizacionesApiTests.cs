using System.Net;
using System.Net.Http.Json;
using HealthAuthorizations.IntegrationTests.Infrastructure;
using static HealthAuthorizations.IntegrationTests.Api.ApiClientExtensions;

namespace HealthAuthorizations.IntegrationTests.Api;

public sealed class AutorizacionesApiTests(DatabaseFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    private async Task<(long Id, string ETag)> CrearAsync()
    {
        using var response = await _client.CrearAsync(CuerpoCrear());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return ((long)(await response.JsonAsync())["id"]!, response.Headers.ETag!.Tag);
    }

    private static object CuerpoActualizar(decimal monto = 2000m) => new
    {
        prestadorId = 7,
        fechaServicio = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        servicios = new[] { new { tipoServicioId = 5, descripcion = "Rayos X", montoReclamado = monto } },
    };

    [Fact]
    public async Task Post_Valido_Devuelve201ConLocationETagYMontos()
    {
        using var response = await _client.CrearAsync(CuerpoCrear(monto: 1500m));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var json = await response.JsonAsync();
        response.Headers.Location!.AbsolutePath.ShouldBe($"{Base}/{json["id"]}");
        response.Headers.ETag.ShouldNotBeNull();
        ((string)json["estado"]!).ShouldBe("Activa");
        ((decimal)json["montoArs"]!).ShouldBe(1050m);
        ((decimal)json["montoAfiliado"]!).ShouldBe(450m);
        ((string)json["numero"]!).ShouldStartWith("AUT-");
    }

    [Fact]
    public async Task Post_RepetidoConIdempotencyKey_DevuelveLaMismaAutorizacionMarcadaComoReintento()
    {
        var clave = Guid.NewGuid().ToString();

        using var primera = await _client.CrearAsync(CuerpoCrear(), clave);
        using var segunda = await _client.CrearAsync(CuerpoCrear(), clave);

        segunda.StatusCode.ShouldBe(HttpStatusCode.Created);
        segunda.Headers.GetValues("Idempotent-Replayed").ShouldBe(["true"]);
        primera.Headers.Contains("Idempotent-Replayed").ShouldBeFalse();
        (await segunda.JsonAsync())["id"]!.ToString().ShouldBe((await primera.JsonAsync())["id"]!.ToString());
    }

    [Fact]
    public async Task Post_Invalido_Devuelve400ConErroresPorCampoEnEspanol()
    {
        using var response = await _client.CrearAsync(new { afiliadoId = 0, prestadorId = 5, fechaServicio = "2026-11-20", servicios = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var json = await response.JsonAsync();
        ((string)json["code"]!).ShouldBe("Solicitud.Invalida");
        json["traceId"].ShouldNotBeNull();
        json["errors"]!["Servicios"].ShouldNotBeNull();
        json["errors"]!["AfiliadoId"]![0]!.ToString().ShouldContain("mayor");
    }

    [Fact]
    public async Task Post_JsonMalformado_Devuelve400ConElMismoContrato()
    {
        using var content = new StringContent("{malo", System.Text.Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync(Base, content, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ((string)(await response.JsonAsync())["code"]!).ShouldBe("Solicitud.Invalida");
    }

    [Fact]
    public async Task Post_ConAfiliadoInexistente_Devuelve422ConCodigo()
    {
        using var response = await _client.CrearAsync(CuerpoCrear(afiliadoId: 999_999));

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        ((string)(await response.JsonAsync())["code"]!).ShouldBe("Afiliado.NoExiste");
    }

    [Fact]
    public async Task Get_Detalle_Devuelve200ConETagY304ConIfNoneMatch()
    {
        var (id, etag) = await CrearAsync();

        using var detalle = await _client.GetAsync($"{Base}/{id}", TestContext.Current.CancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{Base}/{id}");
        request.Headers.TryAddWithoutValidation("If-None-Match", etag);
        using var condicional = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        detalle.StatusCode.ShouldBe(HttpStatusCode.OK);
        detalle.Headers.ETag!.Tag.ShouldBe(etag);
        ((string)(await detalle.JsonAsync())["afiliado"]!["nombre"]!).ShouldBe("Ana Santos Reyes");
        condicional.StatusCode.ShouldBe(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task Get_Inexistente_Devuelve404()
    {
        using var response = await _client.GetAsync($"{Base}/999999999", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ((string)(await response.JsonAsync())["code"]!).ShouldBe("Autorizacion.NoEncontrada");
    }

    [Fact]
    public async Task Put_ConIfMatchVigente_Devuelve200YNuevoETag_ConIfMatchViejo_Devuelve412()
    {
        var (id, etag) = await CrearAsync();

        using var primera = await _client.EnviarAsync(HttpMethod.Put, $"{Base}/{id}", CuerpoActualizar(), etag);
        using var segunda = await _client.EnviarAsync(HttpMethod.Put, $"{Base}/{id}", CuerpoActualizar(3000m), etag);

        primera.StatusCode.ShouldBe(HttpStatusCode.OK);
        primera.Headers.ETag!.Tag.ShouldNotBe(etag);
        ((decimal)(await primera.JsonAsync())["montoArs"]!).ShouldBe(1400m);
        segunda.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        ((string)(await segunda.JsonAsync())["code"]!).ShouldBe("Concurrencia.Conflicto");
    }

    [Fact]
    public async Task Inactivacion_Devuelve204YLuegoBloqueaCambiosCon409()
    {
        var (id, _) = await CrearAsync();

        using var inactivar = await _client.EnviarAsync(HttpMethod.Post, $"{Base}/{id}/inactivacion", new { motivo = "Emitida por error" });
        using var repetir = await _client.EnviarAsync(HttpMethod.Post, $"{Base}/{id}/inactivacion", new { motivo = "Otra vez" });
        using var actualizar = await _client.EnviarAsync(HttpMethod.Put, $"{Base}/{id}", CuerpoActualizar());
        var detalle = await (await _client.GetAsync($"{Base}/{id}", TestContext.Current.CancellationToken)).JsonAsync();

        inactivar.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        repetir.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        actualizar.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ((string)detalle["estado"]!).ShouldBe("Inactiva");
        ((string)detalle["motivoInactivacion"]!).ShouldBe("Emitida por error");
    }

    [Fact]
    public async Task Inactivacion_SinMotivo_Devuelve400()
    {
        var (id, _) = await CrearAsync();

        using var response = await _client.EnviarAsync(HttpMethod.Post, $"{Base}/{id}/inactivacion", new { motivo = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Listar_ConLimiteYCursor_PaginaYUsaLimitePorDefecto()
    {
        var afiliado = await fixture.CrearAfiliadoAsync();
        for (var i = 0; i < 3; i++)
        {
            (await _client.CrearAsync(CuerpoCrear(afiliado))).Dispose();
        }

        var primera = await _client.GetFromJsonAsync<System.Text.Json.Nodes.JsonNode>($"{Base}?afiliadoId={afiliado}&limite=2", TestContext.Current.CancellationToken);
        var segunda = await _client.GetFromJsonAsync<System.Text.Json.Nodes.JsonNode>($"{Base}?afiliadoId={afiliado}&limite=2&cursor={primera!["nextCursor"]}", TestContext.Current.CancellationToken);
        var porDefecto = await _client.GetFromJsonAsync<System.Text.Json.Nodes.JsonNode>($"{Base}?afiliadoId={afiliado}&estado=Activa", TestContext.Current.CancellationToken);

        primera["items"]!.AsArray().Count.ShouldBe(2);
        ((bool)primera["hasMore"]!).ShouldBeTrue();
        segunda!["items"]!.AsArray().Count.ShouldBe(1);
        segunda["nextCursor"].ShouldBeNull();
        ((int)porDefecto!["limit"]!).ShouldBe(20);
        porDefecto["items"]!.AsArray().Count.ShouldBe(3);
    }

    [Theory]
    [InlineData("limite=0")]
    [InlineData("limite=101")]
    [InlineData("cursor=basura")]
    [InlineData("estado=Desconocido")]
    [InlineData("fechaDesde=2026-12-01&fechaHasta=2026-11-01")]
    public async Task Listar_ConParametrosInvalidos_Devuelve400(string query)
    {
        using var response = await _client.GetAsync($"{Base}?{query}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
