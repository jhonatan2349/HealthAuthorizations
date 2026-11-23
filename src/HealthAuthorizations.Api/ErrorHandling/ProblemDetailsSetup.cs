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
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
}
