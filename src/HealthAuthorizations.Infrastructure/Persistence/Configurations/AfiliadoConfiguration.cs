using HealthAuthorizations.Domain.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorizations.Infrastructure.Persistence.Configurations;

public sealed class AfiliadoConfiguration : IEntityTypeConfiguration<Afiliado>
{
    public void Configure(EntityTypeBuilder<Afiliado> builder)
    {
        builder.ToTable("Afiliados");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.NumeroAfiliado).HasMaxLength(Afiliado.MaxLongitudNumeroAfiliado).IsUnicode(false).IsRequired();
        builder.Property(a => a.Documento).HasMaxLength(Afiliado.MaxLongitudDocumento).IsUnicode(false).IsRequired();
        builder.Property(a => a.Nombres).HasMaxLength(Afiliado.MaxLongitudNombres).IsRequired();
        builder.Property(a => a.Apellidos).HasMaxLength(Afiliado.MaxLongitudApellidos).IsRequired();
        builder.Property(a => a.Activo).HasDefaultValue(true);

        builder.HasIndex(a => a.NumeroAfiliado).IsUnique();
        builder.HasIndex(a => a.Documento).IsUnique();

        builder.HasOne(a => a.Contratante)
            .WithMany()
            .HasForeignKey(a => a.ContratanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Intermediario)
            .WithMany()
            .HasForeignKey(a => a.IntermediarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
