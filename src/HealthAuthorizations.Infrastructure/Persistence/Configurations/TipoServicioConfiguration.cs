using HealthAuthorizations.Domain.Catalogos;

namespace HealthAuthorizations.Infrastructure.Persistence.Configurations;

public sealed class TipoServicioConfiguration : CatalogoConfiguration<TipoServicio>
{
    protected override string Tabla => "TiposServicios";
}
