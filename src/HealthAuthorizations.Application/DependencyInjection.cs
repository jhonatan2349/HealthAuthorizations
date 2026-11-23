using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace HealthAuthorizations.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Registra las configuraciones (IRegister) del ensamblado.
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}