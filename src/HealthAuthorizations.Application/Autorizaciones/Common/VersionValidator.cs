using FluentValidation;

namespace HealthAuthorizations.Application.Autorizaciones.Common;

internal static class VersionValidatorExtensions
{
    /// <summary>La versión es opcional, pero si se envía debe ser un token válido.</summary>
    public static IRuleBuilderOptions<T, string?> VersionValida<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(v => v is null || VersionToken.TryDecode(v, out _))
            .WithMessage("La versión (If-Match) no tiene un formato válido.");
}
