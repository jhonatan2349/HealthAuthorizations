using HealthAuthorizations.Domain.Catalogos;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorizations.Infrastructure.Persistence.Configurations;

public sealed class ContratanteConfiguration : CatalogoConfiguration<Contratante>
{
    protected override string Tabla => "Contratantes";

    protected override void ConfigurarEntidad(EntityTypeBuilder<Contratante> builder)
    {
        builder.Property(c => c.Documento).HasMaxLength(Contratante.MaxLongitudDocumento).IsUnicode(false).IsRequired();
        builder.HasIndex(c => c.Documento).IsUnique();
    }
}
