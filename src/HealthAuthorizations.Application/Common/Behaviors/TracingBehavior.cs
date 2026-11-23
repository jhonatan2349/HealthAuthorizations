using System.Diagnostics;
using HealthAuthorizations.Application.Common.Results;
using HealthAuthorizations.Application.Common.Telemetry;
using MediatR;

namespace HealthAuthorizations.Application.Common.Behaviors;

/// <summary>
/// Crea un span por caso de uso, hijo del span HTTP, que agrupa las consultas SQL que ejecuta.
/// Marca el span como error si el resultado falla o hay excepción, con el código de error como etiqueta.
/// </summary>
public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public const string EtiquetaCodigoError = "healthauth.error.code";

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        using var activity = ApplicationTelemetry.ActivitySource.StartActivity(typeof(TRequest).Name);

        try
        {
            var response = await next(cancellationToken);

            if (response is Result { Error: { } error })
            {
                activity?.SetStatus(ActivityStatusCode.Error, error.Code);
                activity?.SetTag(EtiquetaCodigoError, error.Code);
            }

            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            activity?.AddException(ex);
            throw;
        }
    }
}
