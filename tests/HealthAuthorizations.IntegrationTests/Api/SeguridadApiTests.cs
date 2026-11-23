using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using HealthAuthorizations.Api.Security;
using HealthAuthorizations.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using static HealthAuthorizations.IntegrationTests.Api.ApiClientExtensions;

namespace HealthAuthorizations.IntegrationTests.Api;

public sealed class SeguridadApiTests(DatabaseFixture fixture)
{
    private const string Issuer = "https://healthauthorizations.local";
    private const string Audience = "healthauthorizations-api";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string TokenManual(
        string? clave = null,
        string audience = Audience,
        DateTime? expira = null,
        params string[] roles)
    {
        var ahora = DateTime.UtcNow;
        var vence = expira ?? ahora.AddMinutes(10);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            IssuedAt = vence.AddMinutes(-20),
            NotBefore = vence.AddMinutes(-20),
            Expires = vence,
            Claims = new Dictionary<string, object> { ["sub"] = "manual", ["role"] = roles },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave ?? ApiFactory.ClaveFirma)), SecurityAlgorithms.HmacSha256),
        });
    }

    private async Task<HttpResponseMessage> GetConTokenAsync(string token)
    {
        using var client = fixture.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.GetAsync(Base, Ct);
    }

    [Fact]
    public async Task SinToken_Devuelve401ProblemDetailsConDesafioBearer()
    {
        using var client = fixture.CreateAnonymousClient();

        using var response = await client.GetAsync(Base, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ShouldContain(h => h.Scheme == "Bearer");
        ((string)(await response.JsonAsync())["code"]!).ShouldBe("Autenticacion.Requerida");
    }

    [Fact]
    public async Task TokenFirmadoConOtraClave_Devuelve401()
    {
        using var response = await GetConTokenAsync(TokenManual(clave: new string('x', 48), roles: Roles.Operador));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenVencido_Devuelve401()
    {
        using var response = await GetConTokenAsync(TokenManual(expira: DateTime.UtcNow.AddMinutes(-2), roles: Roles.Operador));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenParaOtraAudiencia_Devuelve401()
    {
        using var response = await GetConTokenAsync(TokenManual(audience: "otra-api", roles: Roles.Operador));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenSinRoles_Devuelve403()
    {
        using var response = await GetConTokenAsync(TokenManual());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ((string)(await response.JsonAsync())["code"]!).ShouldBe("Autorizacion.Denegada");
    }

    [Fact]
    public async Task Lector_PuedeConsultarPeroNoEscribir()
    {
        using var lector = fixture.CreateClient(Roles.Lector);

        using var consulta = await lector.GetAsync(Base, Ct);
        using var catalogo = await lector.GetAsync("/api/v1/catalogos/tipos-servicio", Ct);
        using var crear = await lector.CrearAsync(CuerpoCrear());
        using var inactivar = await lector.EnviarAsync(HttpMethod.Post, $"{Base}/1/inactivacion", new { motivo = "x" });

        consulta.StatusCode.ShouldBe(HttpStatusCode.OK);
        catalogo.StatusCode.ShouldBe(HttpStatusCode.OK);
        crear.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        inactivar.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Auditoria_RegistraElSujetoDelToken()
    {
        using var client = fixture.CreateClient();

        using var response = await client.CrearAsync(CuerpoCrear());

        ((string)(await response.JsonAsync())["creadoPor"]!).ShouldBe(DatabaseFixture.SujetoApi);
    }

    [Fact]
    public async Task EmisorDemo_ConCredencialesValidas_EmiteTokenUtilizable()
    {
        using var anonimo = fixture.CreateAnonymousClient();

        using var response = await anonimo.PostAsJsonAsync("/api/v1/auth/token", new { usuario = ApiFactory.UsuarioDemo, clave = ApiFactory.ClaveDemo }, Ct);
        var token = await response.Content.ReadFromJsonAsync<TokenEmitido>(Ct);
        anonimo.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        using var crear = await anonimo.CrearAsync(CuerpoCrear());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        token.TokenType.ShouldBe("Bearer");
        token.ExpiresIn.ShouldBeGreaterThan(0);
        crear.StatusCode.ShouldBe(HttpStatusCode.Created);
        ((string)(await crear.JsonAsync())["creadoPor"]!).ShouldBe(ApiFactory.UsuarioDemo);
    }

    [Theory]
    [InlineData(ApiFactory.UsuarioDemo, "clave-incorrecta")]
    [InlineData("no-existe", ApiFactory.ClaveDemo)]
    [InlineData("", "")]
    public async Task EmisorDemo_ConCredencialesInvalidas_Devuelve401(string usuario, string clave)
    {
        using var anonimo = fixture.CreateAnonymousClient();

        using var response = await anonimo.PostAsJsonAsync("/api/v1/auth/token", new { usuario, clave }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ((string)(await response.JsonAsync())["code"]!).ShouldBe("Autenticacion.CredencialesInvalidas");
    }

    [Fact]
    public async Task EmisorDemo_Deshabilitado_Devuelve404()
    {
        await using var api = fixture.CreateApi(new Dictionary<string, string?> { ["Auth:EmisorDemo:Habilitado"] = "false" });
        using var client = api.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/v1/auth/token", new { usuario = ApiFactory.UsuarioDemo, clave = ApiFactory.ClaveDemo }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EmisorDemo_ExcesoDeIntentos_Devuelve429ConRetryAfter()
    {
        await using var api = fixture.CreateApi(new Dictionary<string, string?> { ["RateLimiting:PermisosTokenPorMinuto"] = "2" });
        using var client = api.CreateClient();
        var cuerpo = new { usuario = ApiFactory.UsuarioDemo, clave = "incorrecta" };

        for (var i = 0; i < 2; i++)
        {
            (await client.PostAsJsonAsync("/api/v1/auth/token", cuerpo, Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using var bloqueada = await client.PostAsJsonAsync("/api/v1/auth/token", cuerpo, Ct);

        bloqueada.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        bloqueada.Headers.RetryAfter.ShouldNotBeNull();
        ((string)(await bloqueada.JsonAsync())["code"]!).ShouldBe("Solicitudes.Excedidas");
    }

    [Fact]
    public async Task LimiteGlobal_SeAplicaPorUsuario()
    {
        await using var api = fixture.CreateApi(new Dictionary<string, string?> { ["RateLimiting:PermisosPorMinuto"] = "3" });
        var emisor = api.Services.GetRequiredService<JwtTokenIssuer>();
        using var usuarioA = api.CreateClient();
        using var usuarioB = api.CreateClient();
        usuarioA.DefaultRequestHeaders.Authorization = new("Bearer", emisor.Emitir("usuario-a", "A", [Roles.Lector]).AccessToken);
        usuarioB.DefaultRequestHeaders.Authorization = new("Bearer", emisor.Emitir("usuario-b", "B", [Roles.Lector]).AccessToken);

        var codigosA = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            using var r = await usuarioA.GetAsync("/api/v1/catalogos/tipos-servicio", Ct);
            codigosA.Add(r.StatusCode);
        }

        using var respuestaB = await usuarioB.GetAsync("/api/v1/catalogos/tipos-servicio", Ct);

        codigosA.ShouldBe([HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests]);
        respuestaB.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Respuestas_IncluyenHeadersDeSeguridadYNoRevelanServidor()
    {
        using var client = fixture.CreateClient();

        using var response = await client.GetAsync(Base, Ct);

        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        response.Headers.GetValues("X-Frame-Options").ShouldBe(["DENY"]);
        response.Headers.GetValues("Content-Security-Policy").Single().ShouldContain("default-src 'none'");
        response.Headers.CacheControl!.Private.ShouldBeTrue();
        response.Headers.Contains("Server").ShouldBeFalse();
    }

    [Fact]
    public async Task CuerpoDemasiadoGrande_KestrelLoRechazaCon413()
    {
        // TestServer no aplica límites de tamaño: esta prueba levanta Kestrel real en un puerto libre.
        await using var api = fixture.CreateApi(new Dictionary<string, string?>());
        api.UseKestrel(0);
        api.StartServer();
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new(
            "Bearer", api.Services.GetRequiredService<JwtTokenIssuer>().Emitir("kestrel", "K", [Roles.Operador]).AccessToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, Base)
        {
            Content = new StringContent($"{{\"diagnostico\":\"{new string('x', 2 * 1024 * 1024)}\"}}", Encoding.UTF8, "application/json"),
        };

        // Con 100-continue el cliente espera la respuesta antes de enviar el cuerpo: Kestrel rechaza por Content-Length
        // sin la carrera de cerrar la conexión mientras el cliente aún escribe (connection reset intermitente).
        request.Headers.ExpectContinue = true;

        using var response = await client.SendAsync(request, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Documentacion_EsPublicaYDeclaraElEsquemaBearer()
    {
        using var anonimo = fixture.CreateAnonymousClient();

        var documento = await anonimo.GetFromJsonAsync<JsonNode>("/openapi/v1.json", Ct);

        documento!["components"]!["securitySchemes"]!["Bearer"]!["scheme"]!.ToString().ShouldBe("bearer");
    }

    [Theory]
    [InlineData("Jwt:SigningKey", "corta", "Testing")]
    [InlineData("Auth:EmisorDemo:Habilitado", "true", "Production")]
    public async Task ConfiguracionInsegura_ImpideElArranque(string clave, string valor, string entorno)
    {
        await using var api = fixture.CreateApi(new Dictionary<string, string?> { [clave] = valor }, entorno);

        Should.Throw<OptionsValidationException>(() => api.CreateClient());
    }
}
