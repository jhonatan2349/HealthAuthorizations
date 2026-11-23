using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.Domain.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorizations.Infrastructure.Persistence.Configurations;

public sealed class AutorizacionConfiguration : IEntityTypeConfiguration<Autorizacion>
{
    public const string SecuenciaHiLo = "AutorizacionesHiLo";

    public void Configure(EntityTypeBuilder<Autorizacion> builder)
    {
        builder.ToTable("Autorizaciones", t =>
        {
            t.HasCheckConstraint(
                "CK_Autorizaciones_Montos",
                "[MontoReclamado] > 0 AND [MontoArs] >= 0 AND [MontoAfiliado] >= 0 AND [MontoArs] + [MontoAfiliado] = [MontoReclamado]");
            t.HasCheckConstraint("CK_Autorizaciones_Estado", "[Estado] IN (1, 2)");
            t.HasCheckConstraint(
                "CK_Autorizaciones_Inactivacion",
                "([Estado] = 1 AND [FechaInactivacion] IS NULL AND [MotivoInactivacion] IS NULL) OR " +
                "([Estado] = 2 AND [FechaInactivacion] IS NOT NULL AND [MotivoInactivacion] IS NOT NULL)");
        });

        builder.HasKey(a => a.Id);

        // HiLo: EF reserva bloques de Ids de una secuencia, así el Id se conoce antes del INSERT
        // (necesario para el Outbox) sin un viaje a la base de datos por cada fila.
        builder.Property(a => a.Id).UseHiLo(SecuenciaHiLo);

        // Número legible derivado del Id, persistido e indexable para búsquedas exactas.
        builder.Property(a => a.Numero)
            .HasMaxLength(Autorizacion.MaxLongitudNumero)
            .IsUnicode(false)
            .HasComputedColumnSql("'AUT-' + RIGHT('0000000000' + CAST([Id] AS varchar(19)), 10)", stored: true);

        builder.Property(a => a.Diagnostico).HasMaxLength(Autorizacion.MaxLongitudDiagnostico);
        builder.Property(a => a.Observaciones).HasMaxLength(Autorizacion.MaxLongitudObservaciones);
        builder.Property(a => a.MotivoInactivacion).HasMaxLength(Autorizacion.MaxLongitudMotivoInactivacion);
        builder.Property(a => a.ClaveIdempotencia).HasMaxLength(Autorizacion.MaxLongitudClaveIdempotencia).IsUnicode(false);
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.Ignore(a => a.DomainEvents);
        builder.Ignore(a => a.EstaActiva);

        builder.HasMany(a => a.Detalles)
            .WithOne()
            .HasForeignKey(d => d.AutorizacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.Detalles).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Afiliado>().WithMany().HasForeignKey(a => a.AfiliadoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Prestador>().WithMany().HasForeignKey(a => a.PrestadorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Contratante>().WithMany().HasForeignKey(a => a.ContratanteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Intermediario>().WithMany().HasForeignKey(a => a.IntermediarioId).OnDelete(DeleteBehavior.Restrict);

        // Índices. En SQL Server cada índice no agrupado incluye la clave agrupada (Id), por lo que
        // "WHERE AfiliadoId = @x ORDER BY Id DESC" se resuelve con un seek sin ordenamiento adicional:
        // base de la paginación por cursor (keyset).
        builder.HasIndex(a => a.Numero).IsUnique();
        builder.HasIndex(a => a.ClaveIdempotencia).IsUnique().HasFilter("[ClaveIdempotencia] IS NOT NULL");
        builder.HasIndex(a => a.AfiliadoId);
        builder.HasIndex(a => a.PrestadorId);
        builder.HasIndex(a => a.ContratanteId);
        builder.HasIndex(a => a.IntermediarioId).HasFilter("[IntermediarioId] IS NOT NULL");
        builder.HasIndex(a => a.FechaServicio);
    }
}
