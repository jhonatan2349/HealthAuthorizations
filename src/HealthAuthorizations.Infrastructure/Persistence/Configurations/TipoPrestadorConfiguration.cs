using HealthAuthorizations.Domain.Catalogos;

namespace HealthAuthorizations.Infrastructure.Persistence.Configurations;

public sealed class TipoPrestadorConfiguration : CatalogoConfiguration<TipoPrestador>
{
    protected override string Tabla => "TiposPrestadores";
}
