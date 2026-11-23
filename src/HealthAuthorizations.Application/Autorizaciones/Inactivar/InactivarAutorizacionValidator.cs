using FluentValidation;
using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Domain.Autorizaciones;

namespace HealthAuthorizations.Application.Autorizaciones.Inactivar;

public sealed class InactivarAutorizacionValidator : AbstractValidator<InactivarAutorizacionCommand>
{
    public InactivarAutorizacionValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Motivo).NotEmpty().MaximumLength(Autorizacion.MaxLongitudMotivoInactivacion);
        RuleFor(x => x.Version).VersionValida();
    }
}
