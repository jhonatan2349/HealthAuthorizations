using HealthAuthorizations.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Autorizaciones.Common;

/// <summary>
/// Lado de lectura (CQRS): proyecta directamente desde las tablas a DTO, sin tracking y en una sola consulta.
/// El agregado no necesita navegaciones a otros agregados solo para mostrarlos.
/// </summary>
internal static class AutorizacionReadModel
{
    public static Task<AutorizacionDto?> ObtenerAsync(IApplicationDbContext db, long id, CancellationToken cancellationToken) =>
        (from a in db.Autorizaciones.AsNoTracking()
         where a.Id == id
         join af in db.Afiliados on a.AfiliadoId equals af.Id
         join p in db.Prestadores on a.PrestadorId equals p.Id
         join c in db.Contratantes on a.ContratanteId equals c.Id
         join i in db.Intermediarios on a.IntermediarioId equals i.Id into intermediarios
         from i in intermediarios.DefaultIfEmpty()
         select new AutorizacionDto(
             a.Id,
             a.Numero,
             a.Estado,
             a.FechaServicio,
             a.Diagnostico,
             a.Observaciones,
             new ReferenciaDto(af.Id, af.NumeroAfiliado, af.Nombres + " " + af.Apellidos),
             new ReferenciaDto(p.Id, p.Codigo, p.Nombre),
             new ReferenciaDto(c.Id, c.Codigo, c.Nombre),
             i == null ? null : new ReferenciaDto(i.Id, i.Codigo, i.Nombre),
             a.MontoReclamado,
             a.MontoArs,
             a.MontoAfiliado,
             a.Detalles
                 .OrderBy(d => d.Id)
                 .Select(d => new ServicioAutorizadoDto(
                     d.Id,
                     new ReferenciaDto(d.TipoServicio!.Id, d.TipoServicio.Codigo, d.TipoServicio.Nombre),
                     d.Descripcion,
                     d.MontoReclamado,
                     d.PorcentajeCobertura,
                     d.MontoArs,
                     d.MontoAfiliado))
                 .ToList(),
             a.FechaInactivacion,
             a.MotivoInactivacion,
             a.CreadoEn,
             a.CreadoPor,
             a.ModificadoEn,
             a.ModificadoPor,
             VersionToken.Encode(a.RowVersion)))
        .FirstOrDefaultAsync(cancellationToken);
}
