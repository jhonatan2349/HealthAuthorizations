using FluentValidation;
using HealthAuthorizations.Application.Autorizaciones.Common;

namespace HealthAuthorizations.Application.Autorizaciones.Actualizar;

public sealed class ActualizarAutorizacionValidator : AbstractValidator<ActualizarAutorizacionCommand>
{
    public ActualizarAutorizacionValidator(TimeProvider timeProvider)
    {
        Include(new DatosAutorizacionValidator(timeProvider));

        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Version).VersionValida();
    }
}
