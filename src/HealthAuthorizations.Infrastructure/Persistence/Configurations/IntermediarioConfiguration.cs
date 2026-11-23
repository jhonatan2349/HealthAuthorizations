using HealthAuthorizations.Domain.Catalogos;

namespace HealthAuthorizations.Infrastructure.Persistence.Configurations;

public sealed class IntermediarioConfiguration : CatalogoConfiguration<Intermediario>
{
    protected override string Tabla => "Intermediarios";
}
