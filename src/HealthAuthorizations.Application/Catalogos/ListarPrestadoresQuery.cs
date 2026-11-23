using FluentValidation;
using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Application.Common.Pagination;
using HealthAuthorizations.Domain.Catalogos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Catalogos;

/// <param name="Buscar">Código o documento exacto, o parte del nombre.</param>
public sealed record ListarPrestadoresQuery(
    int? TipoPrestadorId = null,
    string? Buscar = null,
    bool IncluirInactivos = false,
    string? Cursor = null,
    int Limite = ListarPrestadoresQuery.LimitePorDefecto) : IRequest<CursorPage<PrestadorDto>>, IPaginada
{
    public const int LimitePorDefecto = 50;
    public const int LimiteMaximo = 200;
}

public sealed class ListarPrestadoresValidator : AbstractValidator<ListarPrestadoresQuery>
{
    public ListarPrestadoresValidator()
    {
        Include(new PaginacionValidator(ListarPrestadoresQuery.LimiteMaximo));
        RuleFor(x => x.TipoPrestadorId).GreaterThan(0);
        RuleFor(x => x.Buscar).MaximumLength(Catalogo.MaxLongitudNombre);
    }
}

internal sealed class ListarPrestadoresHandler(IApplicationDbContext db) : IRequestHandler<ListarPrestadoresQuery, CursorPage<PrestadorDto>>
{
    public async Task<CursorPage<PrestadorDto>> Handle(ListarPrestadoresQuery request, CancellationToken cancellationToken)
    {
        var query = db.Prestadores.AsNoTracking().Where(p => request.IncluirInactivos || p.Activo);

        if (Cursor.TryDecode(request.Cursor, out var ultimoId))
        {
            query = query.Where(p => p.Id > ultimoId);
        }

        if (request.TipoPrestadorId is { } tipo)
        {
            query = query.Where(p => p.TipoPrestadorId == tipo);
        }

        if (!string.IsNullOrWhiteSpace(request.Buscar))
        {
            var texto = request.Buscar.Trim();
            query = query.Where(p => p.Codigo == texto || p.Documento == texto || p.Nombre.Contains(texto));
        }

        return await query
            .OrderBy(p => p.Id)
            .Select(p => new PrestadorDto(
                p.Id,
                p.Codigo,
                p.Nombre,
                p.Documento,
                new ReferenciaDto(p.TipoPrestador!.Id, p.TipoPrestador.Codigo, p.TipoPrestador.Nombre),
                p.Activo))
            .ToCursorPageAsync(request.Limite, p => p.Id, cancellationToken);
    }
}
