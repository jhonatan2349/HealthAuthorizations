using FluentValidation;
using HealthAuthorizations.Application.Common.Pagination;
using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.Domain.Catalogos;

namespace HealthAuthorizations.Application.Autorizaciones.Listar;

public sealed class ListarAutorizacionesValidator : AbstractValidator<ListarAutorizacionesQuery>
{
    public ListarAutorizacionesValidator()
    {
        Include(new PaginacionValidator(ListarAutorizacionesQuery.LimiteMaximo));

        RuleFor(x => x)
            .Must(x => x.FechaDesde is null || x.FechaHasta is null || x.FechaDesde <= x.FechaHasta)
            .WithName("FechaDesde")
            .WithMessage("FechaDesde no puede ser posterior a FechaHasta.");

        RuleFor(x => x.Estado).IsInEnum();
        RuleFor(x => x.Numero).MaximumLength(Autorizacion.MaxLongitudNumero);
        RuleFor(x => x.DocumentoAfiliado).MaximumLength(Afiliado.MaxLongitudDocumento);
        RuleFor(x => x.AfiliadoId).GreaterThan(0);
        RuleFor(x => x.PrestadorId).GreaterThan(0);
        RuleFor(x => x.ContratanteId).GreaterThan(0);
        RuleFor(x => x.IntermediarioId).GreaterThan(0);
        RuleFor(x => x.TipoServicioId).GreaterThan(0);
    }
}
