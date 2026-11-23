using HealthAuthorizations.Domain.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorizations.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo común de los catálogos: código único, nombre y estado.</summary>
public abstract class CatalogoConfiguration<TCatalogo> : IEntityTypeConfiguration<TCatalogo>
    where TCatalogo : Catalogo
{
    protected abstract string Tabla { get; }

    public void Configure(EntityTypeBuilder<TCatalogo> builder)
    {
        builder.ToTable(Tabla);
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Codigo).HasMaxLength(Catalogo.MaxLongitudCodigo).IsUnicode(false).IsRequired();
        builder.Property(c => c.Nombre).HasMaxLength(Catalogo.MaxLongitudNombre).IsRequired();
        builder.Property(c => c.Activo).HasDefaultValue(true);

        builder.HasIndex(c => c.Codigo).IsUnique();

        ConfigurarEntidad(builder);
    }

    protected virtual void ConfigurarEntidad(EntityTypeBuilder<TCatalogo> builder)
    {
    }
}
