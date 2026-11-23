using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Infrastructure.HealthChecks;
using HealthAuthorizations.Infrastructure.Messaging;
using HealthAuthorizations.Infrastructure.Persistence;
using HealthAuthorizations.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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

        var timeoutComando = configuration.GetValue("Resiliencia:TimeoutComandoSqlSegundos", 30);

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddSingleton<OutboxInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            options
                .UseSqlServer(connectionString, sql => sql
                    // Reintentos ante fallos transitorios (red, failover, throttling).
                    .EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)
                    .CommandTimeout(timeoutComando))
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                    serviceProvider.GetRequiredService<OutboxInterceptor>()));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        var healthChecks = services.AddHealthChecks()
            .AddCheck("sqlserver", new SqlServerHealthCheck(connectionString), tags: [HealthCheckTags.Ready], timeout: TimeSpan.FromSeconds(5));

        AddMensajeria(services, configuration, healthChecks);

        return services;
    }

    private static void AddMensajeria(IServiceCollection services, IConfiguration configuration, IHealthChecksBuilder healthChecks)
    {
        var seccion = configuration.GetSection(MensajeriaOptions.SectionName);

        services.AddOptions<MensajeriaOptions>()
            .Bind(seccion)
            .Validate(o => o.EsValida, "Configuración de Mensajeria inválida: Host, Usuario, Clave, Exchange y límites del outbox son obligatorios.")
            .ValidateOnStart();

        services.AddMetrics();
        services.AddSingleton<MensajeriaTelemetry>();

        if (!seccion.GetValue<bool>(nameof(MensajeriaOptions.Habilitada)))
        {
            // Sin mensajería los eventos quedan en el outbox y se publicarán cuando se habilite.
            return;
        }

        services.AddSingleton<RabbitMqPublisher>();
        services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<RabbitMqPublisher>());
        services.AddScoped<OutboxDispatcher>();
        services.AddHostedService<OutboxProcessor>();

        healthChecks.AddCheck<RabbitMqHealthCheck>(
            "rabbitmq", HealthStatus.Degraded, tags: [HealthCheckTags.Ready], timeout: TimeSpan.FromSeconds(5));
    }
}
