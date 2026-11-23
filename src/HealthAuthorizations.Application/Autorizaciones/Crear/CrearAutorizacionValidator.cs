using FluentValidation;
using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Domain.Autorizaciones;

namespace HealthAuthorizations.Application.Autorizaciones.Crear;

public sealed class CrearAutorizacionValidator : AbstractValidator<CrearAutorizacionCommand>
{
    public CrearAutorizacionValidator(TimeProvider timeProvider)
    {
        Include(new DatosAutorizacionValidator(timeProvider));

        RuleFor(x => x.AfiliadoId).GreaterThan(0);
        RuleFor(x => x.ClaveIdempotencia).MaximumLength(Autorizacion.MaxLongitudClaveIdempotencia);
    }
}
