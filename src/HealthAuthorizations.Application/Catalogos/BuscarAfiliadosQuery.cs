using FluentValidation;
using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Application.Common.Pagination;
using HealthAuthorizations.Domain.Catalogos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Catalogos;

/// <param name="Buscar">Número de afiliado o documento exacto (índices únicos), o inicio de nombres/apellidos (LIKE con prefijo).</param>
public sealed record BuscarAfiliadosQuery(
    string? Buscar = null,
    int? ContratanteId = null,
    bool IncluirInactivos = false,
    string? Cursor = null,
    int Limite = BuscarAfiliadosQuery.LimitePorDefecto) : IRequest<CursorPage<AfiliadoDto>>, IPaginada
{
    public const int LimitePorDefecto = 20;
    public const int LimiteMaximo = 100;
}

public sealed class BuscarAfiliadosValidator : AbstractValidator<BuscarAfiliadosQuery>
{
    public BuscarAfiliadosValidator()
    {
        Include(new PaginacionValidator(BuscarAfiliadosQuery.LimiteMaximo));
        RuleFor(x => x.ContratanteId).GreaterThan(0);
        RuleFor(x => x.Buscar).MaximumLength(Afiliado.MaxLongitudApellidos);
    }
}

internal sealed class BuscarAfiliadosHandler(IApplicationDbContext db) : IRequestHandler<BuscarAfiliadosQuery, CursorPage<AfiliadoDto>>
{
    public async Task<CursorPage<AfiliadoDto>> Handle(BuscarAfiliadosQuery request, CancellationToken cancellationToken)
    {
        var query = db.Afiliados.AsNoTracking().Where(a => request.IncluirInactivos || a.Activo);

        if (Cursor.TryDecode(request.Cursor, out var ultimoId))
        {
            query = query.Where(a => a.Id > ultimoId);
        }

        if (request.ContratanteId is { } contratante)
        {
            query = query.Where(a => a.ContratanteId == contratante);
        }

        if (!string.IsNullOrWhiteSpace(request.Buscar))
        {
            var texto = request.Buscar.Trim();
            query = query.Where(a =>
                a.NumeroAfiliado == texto || a.Documento == texto || a.Nombres.StartsWith(texto) || a.Apellidos.StartsWith(texto));
        }

        return await query
            .OrderBy(a => a.Id)
            .Select(a => new AfiliadoDto(
                a.Id,
                a.NumeroAfiliado,
                a.Documento,
                a.Nombres,
                a.Apellidos,
                a.FechaNacimiento,
                new ReferenciaDto(a.Contratante!.Id, a.Contratante.Codigo, a.Contratante.Nombre),
                a.Intermediario == null ? null : new ReferenciaDto(a.Intermediario.Id, a.Intermediario.Codigo, a.Intermediario.Nombre),
                a.Activo))
            .ToCursorPageAsync(request.Limite, a => a.Id, cancellationToken);
    }
}
