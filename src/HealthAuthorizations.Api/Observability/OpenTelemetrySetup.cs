using System.Reflection;
using HealthAuthorizations.Application.Common.Telemetry;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HealthAuthorizations.Api.Observability;

/// <summary>
/// Trazas, métricas y logs con OpenTelemetry. La exportación se activa con la variable estándar
/// OTEL_EXPORTER_OTLP_ENDPOINT (Aspire Dashboard, Jaeger, Grafana, Azure Monitor vía collector),
/// sin acoplar el código a un proveedor.
/// </summary>
internal static class OpenTelemetrySetup
{
    public const string NombreServicio = "healthauthorizations-api";
    private const string VariableEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";

    public static WebApplicationBuilder AddApiObservability(this WebApplicationBuilder builder)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        var otel = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(NombreServicio, serviceVersion: version)
                .AddAttributes([new("deployment.environment.name", builder.Environment.EnvironmentName)]))
            .WithTracing(tracing => tracing
                .AddSource(ApplicationTelemetry.Nombre)
                .AddAspNetCoreInstrumentation(options =>
                {
                    // Las sondas de Kubernetes llaman cada pocos segundos: solo agregarían ruido.
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation()
                // No captura el texto SQL por defecto: los parámetros pueden contener datos de salud.
                .AddSqlClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddMeter(ApplicationTelemetry.Nombre)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithLogging(logging => { }, options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
            });

        if (!string.IsNullOrWhiteSpace(builder.Configuration[VariableEndpoint]))
        {
            otel.UseOtlpExporter();
        }

        return builder;
    }
}
