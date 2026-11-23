using System.Diagnostics.CodeAnalysis;

namespace HealthAuthorizations.Application.Common.Results;

/// <summary>Error esperado del negocio. El código es estable para que los clientes puedan reaccionar a él.</summary>
[SuppressMessage("Naming", "CA1716", Justification = "Solución exclusivamente C#; Error es el nombre idiomático del Result pattern.")]
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error BusinessRule(string code, string message) => new(code, message, ErrorType.BusinessRule);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
}
