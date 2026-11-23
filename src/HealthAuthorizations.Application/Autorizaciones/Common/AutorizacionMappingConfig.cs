using HealthAuthorizations.Domain.Autorizaciones;
using Mapster;

namespace HealthAuthorizations.Application.Autorizaciones.Common;

/// <summary>Mapeo de contratos de entrada a los objetos de valor del dominio.</summary>
public sealed class AutorizacionMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.NewConfig<ServicioRequest, ServicioSolicitado>()
            .MapToConstructor(true);
    }
}
