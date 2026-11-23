using HealthAuthorizations.Domain.Autorizaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorizations.Infrastructure.Persistence.Configurations;

public sealed class AutorizacionDetalleConfiguration : IEntityTypeConfiguration<AutorizacionDetalle>
{
    public void Configure(EntityTypeBuilder<AutorizacionDetalle> builder)
    {
        builder.ToTable("AutorizacionesDetalle", t =>
        {
            t.HasCheckConstraint(
                "CK_AutorizacionesDetalle_Montos",
                "[MontoReclamado] > 0 AND [MontoArs] >= 0 AND [MontoAfiliado] >= 0 AND [MontoArs] + [MontoAfiliado] = [MontoReclamado]");
            t.HasCheckConstraint("CK_AutorizacionesDetalle_Porcentaje", "[PorcentajeCobertura] BETWEEN 0 AND 100");
        });

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Descripcion).HasMaxLength(AutorizacionDetalle.MaxLongitudDescripcion).IsRequired();
        builder.Property(d => d.PorcentajeCobertura).HasPrecision(5, 2);

        builder.HasOne(d => d.TipoServicio)
            .WithMany()
            .HasForeignKey(d => d.TipoServicioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
