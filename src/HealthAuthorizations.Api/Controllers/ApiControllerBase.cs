using HealthAuthorizations.Api.ErrorHandling;
using HealthAuthorizations.Application.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HealthAuthorizations.Api.Controllers;

/// <summary>Base de los controllers: traduce los errores esperados (Result) a ProblemDetails.</summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;

    protected ObjectResult Problema(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, ProblemTitles.Validacion),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, ProblemTitles.NoEncontrado),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, ProblemTitles.Conflicto),
            _ => (StatusCodes.Status422UnprocessableEntity, ProblemTitles.ReglaDeNegocio),
        };

        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, title, detail: error.Message);
        problem.Extensions[ProblemTitles.ExtensionCodigo] = error.Code;

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
