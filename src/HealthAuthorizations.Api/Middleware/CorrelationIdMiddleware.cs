using System.Diagnostics;

namespace HealthAuthorizations.Api.Middleware;

/// <summary>
/// Propaga un identificador de correlación por solicitud: lo toma del header X-Correlation-Id
/// (si es seguro) o usa el TraceId de la actividad actual. Se devuelve en la respuesta, se agrega
/// al scope de logging y es el traceId de los ProblemDetails, de modo que un incidente reportado
/// por un cliente se ubica directamente en los logs.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    private const int MaxLongitud = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var recibido = context.Request.Headers[HeaderName].ToString();
        var correlationId = EsValido(recibido)
            ? recibido
            : Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    /// <summary>Evita inyección en headers y logs: solo alfanuméricos, guiones y guiones bajos.</summary>
    internal static bool EsValido(string? valor) =>
        !string.IsNullOrEmpty(valor)
        && valor.Length <= MaxLongitud
        && valor.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
