using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace HealthAuthorizations.IntegrationTests.Api;

internal static class ApiClientExtensions
{
    public const string Base = "/api/v1/autorizaciones";

    public static object CuerpoCrear(int afiliadoId = 3, decimal monto = 1000m) => new
    {
        afiliadoId,
        prestadorId = 5,
        fechaServicio = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        diagnostico = "Fiebre",
        servicios = new[] { new { tipoServicioId = 1, descripcion = "Consulta", montoReclamado = monto } },
    };

    public static async Task<HttpResponseMessage> CrearAsync(this HttpClient client, object cuerpo, string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Base) { Content = JsonContent.Create(cuerpo) };
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static async Task<HttpResponseMessage> EnviarAsync(this HttpClient client, HttpMethod method, string url, object? cuerpo = null, string? ifMatch = null)
    {
        using var request = new HttpRequestMessage(method, url) { Content = cuerpo is null ? null : JsonContent.Create(cuerpo) };
        if (ifMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static async Task<JsonNode> JsonAsync(this HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
}
