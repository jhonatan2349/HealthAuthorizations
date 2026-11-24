using System.Net;
using System.Net.Http.Json;
using HealthAuthorizations.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Options;

namespace HealthAuthorizations.IntegrationTests.Api;

/// <summary>
/// X-Forwarded-For solo se acepta desde redes confiables. Se usa Kestrel real porque TestServer no informa
/// la IP remota y el middleware aceptaría el encabezado sin verificar su origen.
/// La política de tokens (rate limiting por IP) permite observar qué IP considera la API.
/// </summary>
public sealed class ProxyInversoApiTests(DatabaseFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly object CredencialInvalida = new { usuario = ApiFactory.UsuarioDemo, clave = "incorrecta" };

    private static async Task<HttpStatusCode> PedirTokenDesdeAsync(HttpClient client, string ipCliente)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/token")
        {
            Content = JsonContent.Create(CredencialInvalida),
        };
        request.Headers.Add("X-Forwarded-For", ipCliente);
        using var response = await client.SendAsync(request, Ct);
        return response.StatusCode;
    }

    private ApiFactory ApiConProxy(string redConfiable)
    {
        var api = fixture.CreateApi(new Dictionary<string, string?>
        {
            ["ProxyInverso:Habilitado"] = "true",
            ["ProxyInverso:RedesConfiables:0"] = redConfiable,
            ["RateLimiting:PermisosTokenPorMinuto"] = "1",
        });
        api.UseKestrel(0);
        api.StartServer();
        return api;
    }

    [Fact]
    public async Task ProxyConfiable_LaIpRealDelClienteDefineElLimite()
    {
        await using var api = ApiConProxy("127.0.0.0/8");
        using var client = api.CreateClient();

        (await PedirTokenDesdeAsync(client, "203.0.113.10")).ShouldBe(HttpStatusCode.Unauthorized);
        (await PedirTokenDesdeAsync(client, "203.0.113.20")).ShouldBe(HttpStatusCode.Unauthorized);
        (await PedirTokenDesdeAsync(client, "203.0.113.10")).ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task OrigenNoConfiable_IgnoraXForwardedFor()
    {
        // El cliente conecta desde loopback, que no está en la red declarada: falsear la IP no evade el límite.
        await using var api = ApiConProxy("10.0.0.0/8");
        using var client = api.CreateClient();

        (await PedirTokenDesdeAsync(client, "203.0.113.10")).ShouldBe(HttpStatusCode.Unauthorized);
        (await PedirTokenDesdeAsync(client, "203.0.113.20")).ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("no-es-una-red")]
    public async Task ConfiguracionInsegura_ImpideElArranque(string? red)
    {
        await using var api = fixture.CreateApi(new Dictionary<string, string?>
        {
            ["ProxyInverso:Habilitado"] = "true",
            ["ProxyInverso:RedesConfiables:0"] = red,
        });

        Should.Throw<OptionsValidationException>(() => api.CreateClient());
    }
}
