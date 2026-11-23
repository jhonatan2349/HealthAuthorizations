using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Application.Common.Results;
using HealthAuthorizations.Domain.Autorizaciones;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Autorizaciones.Common;

/// <summary>Carga el agregado para modificarlo, aplicando las precondiciones comunes de los comandos.</summary>
internal static class AutorizacionTracking
{
    public static async Task<Result<Autorizacion>> CargarActivaAsync(
        IApplicationDbContext db,
        long id,
        string? versionEsperada,
        CancellationToken cancellationToken)
    {
        var autorizacion = await db.Autorizaciones
            .Include(a => a.Detalles)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (autorizacion is null)
        {
            return AutorizacionErrors.NoEncontrada(id);
        }

        if (!autorizacion.EstaActiva)
        {
            return AutorizacionErrors.Inactiva(id);
        }

        // Concurrencia optimista: EF compara este valor en el WHERE del UPDATE.
        // Si otro usuario modificó la autorización, SaveChanges lanza ConcurrencyConflictException.
        if (VersionToken.TryDecode(versionEsperada, out var rowVersion))
        {
            db.Entry(autorizacion).Property(a => a.RowVersion).OriginalValue = rowVersion;
        }

        return autorizacion;
    }
}
