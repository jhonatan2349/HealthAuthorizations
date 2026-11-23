using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Autorizaciones.Common;

/// <summary>
/// Verifica que las entidades referenciadas existan y estén activas antes de crear o actualizar.
/// Las FKs de la base de datos siguen siendo la garantía final; esto produce errores claros para el cliente.
/// </summary>
internal sealed class ReferenciasAutorizacion(IApplicationDbContext db)
{
    public async Task<Error?> ValidarAsync(IDatosAutorizacion datos, CancellationToken cancellationToken)
    {
        var prestador = await db.Prestadores
            .AsNoTracking()
            .Where(p => p.Id == datos.PrestadorId)
            .Select(p => new { p.Activo })
            .FirstOrDefaultAsync(cancellationToken);

        if (prestador is null)
        {
            return AutorizacionErrors.PrestadorNoExiste(datos.PrestadorId);
        }

        if (!prestador.Activo)
        {
            return AutorizacionErrors.PrestadorInactivo(datos.PrestadorId);
        }

        var solicitados = datos.Servicios.Select(s => s.TipoServicioId).Distinct().ToList();

        var existentes = await db.TiposServicios
            .AsNoTracking()
            .Where(t => solicitados.Contains(t.Id) && t.Activo)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var faltantes = solicitados.Except(existentes).Order().ToList();

        return faltantes.Count > 0 ? AutorizacionErrors.TiposServicioNoExisten(faltantes) : null;
    }
}
