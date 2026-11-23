using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthAuthorizations.IntegrationTests.Infrastructure;

namespace HealthAuthorizations.IntegrationTests.Api;

/// <summary>Catálogos, correlación, documentación y rutas inexistentes.</summary>
public sealed class PlataformaApiTests(DatabaseFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Theory]
    [InlineData("tipos-servicio", 5)]
    [InlineData("tipos-prestador", 4)]
    [InlineData("contratantes", 4)]
    [InlineData("intermediarios", 3)]
    public async Task CatalogosSimples_DevuelvenElSeed(string catalogo, int minimo)
    {
        var items = await _client.GetFromJsonAsync<JsonArray>($"/api/v1/catalogos/{catalogo}", TestContext.Current.CancellationToken);

        items!.Count.ShouldBeGreaterThanOrEqualTo(minimo);
        items.ShouldAllBe(i => (bool)i!["activo"]!);
    }

    [Fact]
    public async Task Prestadores_FiltraPorTipoYPagina()
    {
        var farmacias = await _client.GetFromJsonAsync<JsonNode>("/api/v1/catalogos/prestadores?tipoPrestadorId=2", TestContext.Current.CancellationToken);
        var pagina = await _client.GetFromJsonAsync<JsonNode>("/api/v1/catalogos/prestadores?limite=3", TestContext.Current.CancellationToken);

        farmacias!["items"]!.AsArray().ShouldAllBe(p => (string)p!["tipoPrestador"]!["codigo"]! == "FAR");
        pagina!["items"]!.AsArray().Count.ShouldBe(3);
        ((bool)pagina["hasMore"]!).ShouldBeTrue();
    }

    [Theory]
    [InlineData("AF-000004")]
    [InlineData("Castillo")]
    [InlineData("40200000004")]
    public async Task Afiliados_SeBuscanPorNumeroApellidoODocumento(string buscar)
    {
        var resultado = await _client.GetFromJsonAsync<JsonNode>($"/api/v1/catalogos/afiliados?buscar={buscar}", TestContext.Current.CancellationToken);

        var afiliado = resultado!["items"]!.AsArray().ShouldHaveSingleItem()!;
        ((string)afiliado["numeroAfiliado"]!).ShouldBe("AF-000004");
        ((string)afiliado["contratante"]!["codigo"]!).ShouldBe("CTR-002");
    }

    [Fact]
    public async Task CorrelationId_SeRespetaSiEsValidoYSeReemplazaSiNo()
    {
        using var valido = new HttpRequestMessage(HttpMethod.Get, "/api/v1/catalogos/contratantes");
        valido.Headers.Add("X-Correlation-Id", "demo-123");
        using var malicioso = new HttpRequestMessage(HttpMethod.Get, "/api/v1/catalogos/contratantes");
        malicioso.Headers.TryAddWithoutValidation("X-Correlation-Id", "<script>alert(1)</script>");

        using var r1 = await _client.SendAsync(valido, TestContext.Current.CancellationToken);
        using var r2 = await _client.SendAsync(malicioso, TestContext.Current.CancellationToken);

        r1.Headers.GetValues("X-Correlation-Id").ShouldBe(["demo-123"]);
        r2.Headers.GetValues("X-Correlation-Id").Single().ShouldNotContain("<");
    }

    [Fact]
    public async Task ErrorIncluyeElCorrelationIdComoTraceId()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/autorizaciones/999999999");
        request.Headers.Add("X-Correlation-Id", "incidente-42");

        using var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        ((string)(await response.Content.ReadFromJsonAsync<JsonNode>(TestContext.Current.CancellationToken))!["traceId"]!).ShouldBe("incidente-42");
    }

    [Fact]
    public async Task RutaInexistente_Devuelve404ProblemDetails()
    {
        using var response = await _client.GetAsync("/api/v1/no-existe", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task DocumentoOpenApi_DescribeLosEndpoints()
    {
        var documento = await _client.GetFromJsonAsync<JsonNode>("/openapi/v1.json", TestContext.Current.CancellationToken);

        ((string)documento!["info"]!["title"]!).ShouldBe("HealthAuthorizations API");
        documento["paths"]!["/api/v1/autorizaciones"].ShouldNotBeNull();
        documento["paths"]!["/api/v1/autorizaciones/{id}/inactivacion"].ShouldNotBeNull();
    }
}
