using FluentValidation;
using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Common.Behaviors;
using HealthAuthorizations.Application.Common.Options;
using HealthAuthorizations.Domain.Cobertura;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HealthAuthorizations.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var assembly = typeof(DependencyInjection).Assembly;

        // Mapster: registra las configuraciones (IRegister) del ensamblado.
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        // MediatR: el orden de los behaviors define el pipeline (logging envuelve la validación).
        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = configuration["MediatR:LicenseKey"];
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Los validadores de interfaces (IPaginada, IDatosAutorizacion) son parciales: se componen con Include()
        // en el validador de cada solicitud y no se registran por sí solos.
        services.AddValidatorsFromAssembly(
            assembly,
            includeInternalTypes: true,
            filter: r => !r.InterfaceType.GenericTypeArguments[0].IsInterface);
        services.AddScoped<ReferenciasAutorizacion>();

        // Regla de cobertura configurable; se valida al arrancar para fallar rápido.
        services.AddOptions<CoberturaOptions>()
            .BindConfiguration(CoberturaOptions.SectionName)
            .Validate(o => o.EsValida, "Cobertura:PorcentajeArs debe estar entre 0 y 100.")
            .ValidateOnStart();

        services.AddSingleton<IPoliticaCobertura>(sp =>
            new PoliticaCoberturaPorcentual(sp.GetRequiredService<IOptions<CoberturaOptions>>().Value.PorcentajeArs));

        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
