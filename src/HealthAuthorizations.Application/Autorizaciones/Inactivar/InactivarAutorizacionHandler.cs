using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Application.Common.Results;
using HealthAuthorizations.Application.Common.Telemetry;
using MediatR;

namespace HealthAuthorizations.Application.Autorizaciones.Inactivar;

internal sealed class InactivarAutorizacionHandler(IApplicationDbContext db, TimeProvider timeProvider, ApplicationTelemetry telemetry)
    : IRequestHandler<InactivarAutorizacionCommand, Result>
{
    public async Task<Result> Handle(InactivarAutorizacionCommand request, CancellationToken cancellationToken)
    {
        var carga = await AutorizacionTracking.CargarActivaAsync(db, request.Id, request.Version, cancellationToken);
        if (carga.IsFailure)
        {
            return carga.Error!;
        }

        carga.Value.Inactivar(request.Motivo, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        telemetry.AutorizacionInactivada();

        return Result.Success();
    }
}
