using FluentValidation;

namespace HealthAuthorizations.Application.Common.Pagination;

/// <summary>Reglas comunes de paginación; se incluye en el validador de cada consulta paginada.</summary>
public sealed class PaginacionValidator : AbstractValidator<IPaginada>
{
    public PaginacionValidator(int limiteMaximo)
    {
        RuleFor(x => x.Limite).InclusiveBetween(1, limiteMaximo);

        RuleFor(x => x.Cursor)
            .Must(c => c is null || Cursor.TryDecode(c, out _))
            .WithMessage("El cursor no es válido. Use el valor NextCursor de la página anterior.");
    }
}
