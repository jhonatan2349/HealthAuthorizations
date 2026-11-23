using System.Diagnostics;
using HealthAuthorizations.Application.Common.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HealthAuthorizations.Application.Common.Behaviors;

/// <summary>
/// Registra cada caso de uso con su duración y resultado, y advierte cuando supera el umbral.
/// No registra el contenido de la solicitud: contiene datos de salud (PII).
/// </summary>
public sealed partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    internal const int UmbralLentoMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var nombre = typeof(TRequest).Name;
        var inicio = Stopwatch.GetTimestamp();

        var response = await next(cancellationToken);

        var duracionMs = (long)Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;

        if (response is Result { Error: { } error })
        {
            LogFallido(nombre, duracionMs, error.Code);
        }
        else
        {
            LogCompletado(nombre, duracionMs);
        }

        if (duracionMs > UmbralLentoMs)
        {
            LogLento(nombre, duracionMs, UmbralLentoMs);
        }

        return response;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Request} completado en {DuracionMs} ms")]
    private partial void LogCompletado(string request, long duracionMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Request} terminó con error {CodigoError} en {DuracionMs} ms")]
    private partial void LogFallido(string request, long duracionMs, string codigoError);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Request} lento: {DuracionMs} ms (umbral {UmbralMs} ms)")]
    private partial void LogLento(string request, long duracionMs, int umbralMs);
}
