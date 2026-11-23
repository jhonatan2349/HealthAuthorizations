using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Application.Common.Results;
using MediatR;

namespace HealthAuthorizations.Application.Autorizaciones.Obtener;

public sealed record ObtenerAutorizacionQuery(long Id) : IRequest<Result<AutorizacionDto>>;

internal sealed class ObtenerAutorizacionHandler(IApplicationDbContext db)
    : IRequestHandler<ObtenerAutorizacionQuery, Result<AutorizacionDto>>
{
    public async Task<Result<AutorizacionDto>> Handle(ObtenerAutorizacionQuery request, CancellationToken cancellationToken) =>
        await AutorizacionReadModel.ObtenerAsync(db, request.Id, cancellationToken) is { } dto
            ? dto
            : AutorizacionErrors.NoEncontrada(request.Id);
}
