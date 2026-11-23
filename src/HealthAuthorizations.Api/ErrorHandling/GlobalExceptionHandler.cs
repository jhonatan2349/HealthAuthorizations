using FluentValidation;
using HealthAuthorizations.Application.Common.Exceptions;
using HealthAuthorizations.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace HealthAuthorizations.Api.ErrorHandling;

/// <summary>
/// Traduce las excepciones a ProblemDetails (RFC 9457) con un formato único para toda la API.
/// Los errores 5xx nunca exponen detalles internos; se registran con el traceId para analizarlos.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // El cliente canceló la solicitud: no es un error del servidor.
            LogCancelada(httpContext.Request.Path.Value);
            return true;
        }

        var problem = Crear(httpContext, exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            LogError(exception, httpContext.Request.Method, httpContext.Request.Path.Value);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Crear(HttpContext httpContext, Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(
            validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ProblemTitles.Validacion,
            Extensions = { [ProblemTitles.ExtensionCodigo] = ProblemTitles.CodigoValidacion },
        },

        // Con If-Match el cliente pidió una precondición explícita: 412. Sin ella, la carrera se informa como 409.
        ConcurrencyConflictException => Problema(
            httpContext.Request.Headers.ContainsKey(HeaderNames.IfMatch) ? StatusCodes.Status412PreconditionFailed : StatusCodes.Status409Conflict,
            ProblemTitles.VersionDesactualizada,
            ProblemTitles.CodigoConcurrencia,
            "La autorización fue modificada por otra operación. Consulte la versión actual e intente de nuevo."),

        DuplicateKeyException => Problema(
            StatusCodes.Status409Conflict, ProblemTitles.Conflicto, ProblemTitles.CodigoDuplicado, "Ya existe un registro con los mismos datos únicos."),

        // Los mensajes de dominio están pensados para el usuario: es seguro exponerlos.
        DomainException domain => Problema(
            StatusCodes.Status422UnprocessableEntity, ProblemTitles.ReglaDeNegocio, ProblemTitles.CodigoDominio, domain.Message),

        BadHttpRequestException bad => Problema(
            bad.StatusCode, ProblemTitles.Validacion, ProblemTitles.CodigoValidacion, "La solicitud no tiene un formato válido."),

        _ => Problema(
            StatusCodes.Status500InternalServerError, ProblemTitles.Interno, ProblemTitles.CodigoInterno, "Ocurrió un error inesperado. Use el traceId para reportarlo."),
    };

    private static ProblemDetails Problema(int status, string title, string code, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Extensions = { [ProblemTitles.ExtensionCodigo] = code },
    };

    [LoggerMessage(Level = LogLevel.Error, Message = "Error no controlado en {Method} {Path}")]
    private partial void LogError(Exception exception, string method, string? path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Solicitud cancelada por el cliente: {Path}")]
    private partial void LogCancelada(string? path);
}
