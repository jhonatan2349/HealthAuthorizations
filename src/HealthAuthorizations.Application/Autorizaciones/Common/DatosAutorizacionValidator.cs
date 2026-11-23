using FluentValidation;
using HealthAuthorizations.Domain.Autorizaciones;

namespace HealthAuthorizations.Application.Autorizaciones.Common;

/// <summary>Reglas de forma compartidas por crear y actualizar. Las de existencia se validan en el handler.</summary>
public sealed class DatosAutorizacionValidator : AbstractValidator<IDatosAutorizacion>
{
    /// <summary>Se permite pre-autorizar servicios programados hasta este número de días.</summary>
    public const int MaxDiasFuturo = 30;

    public DatosAutorizacionValidator(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        RuleFor(x => x.PrestadorId).GreaterThan(0);

        RuleFor(x => x.FechaServicio)
            .NotEmpty()
            .Must(fecha => fecha <= DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddDays(MaxDiasFuturo))
            .WithMessage($"La fecha del servicio no puede superar {MaxDiasFuturo} días en el futuro.");

        RuleFor(x => x.Diagnostico).MaximumLength(Autorizacion.MaxLongitudDiagnostico);
        RuleFor(x => x.Observaciones).MaximumLength(Autorizacion.MaxLongitudObservaciones);

        RuleFor(x => x.Servicios)
            .NotEmpty().WithMessage("La autorización debe contener al menos un servicio.")
            .Must(s => s is null || s.Count <= Autorizacion.MaxServicios)
            .WithMessage($"La autorización no puede contener más de {Autorizacion.MaxServicios} servicios.");

        RuleForEach(x => x.Servicios).SetValidator(new ServicioRequestValidator());
    }
}

public sealed class ServicioRequestValidator : AbstractValidator<ServicioRequest>
{
    public ServicioRequestValidator()
    {
        RuleFor(x => x.TipoServicioId).GreaterThan(0);
        RuleFor(x => x.Descripcion).NotEmpty().MaximumLength(AutorizacionDetalle.MaxLongitudDescripcion);
        RuleFor(x => x.MontoReclamado)
            .GreaterThan(0)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true);
    }
}
