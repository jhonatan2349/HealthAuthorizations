using HealthAuthorizations.Domain.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorizations.Infrastructure.Persistence.Configurations;

public sealed class PrestadorConfiguration : CatalogoConfiguration<Prestador>
{
    protected override string Tabla => "Prestadores";

    protected override void ConfigurarEntidad(EntityTypeBuilder<Prestador> builder)
    {
        builder.Property(p => p.Documento).HasMaxLength(Prestador.MaxLongitudDocumento).IsUnicode(false).IsRequired();
        builder.HasIndex(p => p.Documento).IsUnique();

        builder.HasOne(p => p.TipoPrestador)
            .WithMany()
            .HasForeignKey(p => p.TipoPrestadorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
