using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Application.Common.Results;
using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.Domain.Cobertura;
using MapsterMapper;
using MediatR;

namespace HealthAuthorizations.Application.Autorizaciones.Actualizar;

internal sealed class ActualizarAutorizacionHandler(
    IApplicationDbContext db,
    IPoliticaCobertura politica,
    ReferenciasAutorizacion referencias,
    IMapper mapper) : IRequestHandler<ActualizarAutorizacionCommand, Result<AutorizacionDto>>
{
    public async Task<Result<AutorizacionDto>> Handle(ActualizarAutorizacionCommand request, CancellationToken cancellationToken)
    {
        var carga = await AutorizacionTracking.CargarActivaAsync(db, request.Id, request.Version, cancellationToken);
        if (carga.IsFailure)
        {
            return carga.Error!;
        }

        if (await referencias.ValidarAsync(request, cancellationToken) is { } error)
        {
            return error;
        }

        carga.Value.Actualizar(
            request.PrestadorId,
            request.FechaServicio,
            request.Diagnostico,
            request.Observaciones,
            mapper.Map<List<ServicioSolicitado>>(request.Servicios),
            politica);

        await db.SaveChangesAsync(cancellationToken);

        return (await AutorizacionReadModel.ObtenerAsync(db, request.Id, cancellationToken))!;
    }
}
