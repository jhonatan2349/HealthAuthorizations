using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Common.Exceptions;
using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Application.Common.Results;
using HealthAuthorizations.Application.Common.Telemetry;
using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.Domain.Cobertura;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Autorizaciones.Crear;

internal sealed class CrearAutorizacionHandler(
    IApplicationDbContext db,
    IPoliticaCobertura politica,
    ReferenciasAutorizacion referencias,
    IMapper mapper,
    ApplicationTelemetry telemetry) : IRequestHandler<CrearAutorizacionCommand, Result<CrearAutorizacionResult>>
{
    public async Task<Result<CrearAutorizacionResult>> Handle(CrearAutorizacionCommand request, CancellationToken cancellationToken)
    {
        // 1. Reintento: la misma clave devuelve la autorización ya creada, sin duplicar.
        if (request.ClaveIdempotencia is not null)
        {
            var existente = await ReintentoAsync(request, cancellationToken);
            if (existente is not null)
            {
                return existente;
            }
        }

        // 2. El contratante y el intermediario se toman del afiliado (snapshot).
        var afiliado = await db.Afiliados
            .AsNoTracking()
            .Where(a => a.Id == request.AfiliadoId)
            .Select(a => new { a.Activo, a.ContratanteId, a.IntermediarioId, ContratanteActivo = a.Contratante!.Activo })
            .FirstOrDefaultAsync(cancellationToken);

        if (afiliado is null)
        {
            return AutorizacionErrors.AfiliadoNoExiste(request.AfiliadoId);
        }

        if (!afiliado.Activo)
        {
            return AutorizacionErrors.AfiliadoInactivo(request.AfiliadoId);
        }

        if (!afiliado.ContratanteActivo)
        {
            return AutorizacionErrors.ContratanteInactivo(afiliado.ContratanteId);
        }

        if (await referencias.ValidarAsync(request, cancellationToken) is { } error)
        {
            return error;
        }

        var autorizacion = Autorizacion.Crear(
            new Participantes(request.AfiliadoId, request.PrestadorId, afiliado.ContratanteId, afiliado.IntermediarioId),
            request.FechaServicio,
            request.Diagnostico,
            request.Observaciones,
            mapper.Map<List<ServicioSolicitado>>(request.Servicios),
            politica,
            request.ClaveIdempotencia);

        db.Autorizaciones.Add(autorizacion);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException) when (request.ClaveIdempotencia is not null)
        {
            // 3. Carrera: otra solicitud con la misma clave se guardó primero; el índice único lo impidió.
            return await ReintentoAsync(request, cancellationToken)
                ?? throw new InvalidOperationException("Clave de idempotencia duplicada sin autorización asociada.");
        }

        telemetry.AutorizacionCreada(autorizacion.MontoReclamado);

        var dto = await AutorizacionReadModel.ObtenerAsync(db, autorizacion.Id, cancellationToken);
        return new CrearAutorizacionResult(dto!, EsReintento: false);
    }

    private async Task<Result<CrearAutorizacionResult>?> ReintentoAsync(CrearAutorizacionCommand request, CancellationToken cancellationToken)
    {
        var original = await db.Autorizaciones
            .AsNoTracking()
            .Where(a => a.ClaveIdempotencia == request.ClaveIdempotencia)
            .Select(a => new { a.Id, a.AfiliadoId })
            .FirstOrDefaultAsync(cancellationToken);

        if (original is null)
        {
            return null;
        }

        // Misma clave para otro afiliado: es un error del cliente, no un reintento.
        if (original.AfiliadoId != request.AfiliadoId)
        {
            return AutorizacionErrors.ClaveIdempotenciaEnUso();
        }

        telemetry.ReintentoIdempotente();

        var dto = await AutorizacionReadModel.ObtenerAsync(db, original.Id, cancellationToken);
        return new CrearAutorizacionResult(dto!, EsReintento: true);
    }
}
