using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Infrastructure.Persistence;
using HealthAuthorizations.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthAuthorizations.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Falta la cadena de conexión 'ConnectionStrings:{ConnectionStringName}'. " +
                "Configúrela con user-secrets (desarrollo) o la variable de entorno ConnectionStrings__Default.");
        }

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            options
                .UseSqlServer(connectionString, sql =>
                    // Reintentos ante fallos transitorios (red, failover, throttling).
                    sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null))
                .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}
