using System.Diagnostics;

namespace HealthAuthorizations.Api.ErrorHandling;

internal static class ProblemDetailsSetup
{
    /// <summary>Agrega a todo ProblemDetails (excepciones, validación de modelo, Results) la instancia y el traceId.</summary>
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var http = context.HttpContext;
            context.ProblemDetails.Instance ??= $"{http.Request.Method} {http.Request.Path}";
            context.ProblemDetails.Extensions[ProblemTitles.ExtensionTraceId] = http.TraceIdentifier;
            context.ProblemDetails.Extensions.TryAdd("activityId", Activity.Current?.Id);

            // Errores generados por el framework (JSON malformado, binding, 404 de ruta) con el mismo contrato que los propios.
            if (!context.ProblemDetails.Extensions.ContainsKey(ProblemTitles.ExtensionCodigo))
            {
                var (title, code) = context.ProblemDetails.Status switch
                {
                    StatusCodes.Status400BadRequest => (ProblemTitles.Validacion, ProblemTitles.CodigoValidacion),
                    StatusCodes.Status401Unauthorized => ("No autenticado", "Autenticacion.Requerida"),
                    StatusCodes.Status403Forbidden => ("Acceso denegado", "Autorizacion.Denegada"),
                    StatusCodes.Status404NotFound => (ProblemTitles.NoEncontrado, "Recurso.NoEncontrado"),
                    _ => (context.ProblemDetails.Title, "Http.Error"),
                };
                context.ProblemDetails.Title = title;
                context.ProblemDetails.Extensions[ProblemTitles.ExtensionCodigo] = code;
            }
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
}
