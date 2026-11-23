using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthAuthorizations.Infrastructure.HealthChecks;

/// <summary>Etiquetas para agrupar las comprobaciones por tipo de sonda.</summary>
public static class HealthCheckTags
{
    /// <summary>Dependencias necesarias para atender tráfico (readiness).</summary>
    public const string Ready = "ready";
}

/// <summary>
/// Comprueba que SQL Server responde. Usa una conexión directa y sin reintentos: la sonda debe
/// responder rápido y el reintento lo hace el orquestador (Kubernetes vuelve a consultar).
/// </summary>
internal sealed class SqlServerHealthCheck(string connectionString) : IHealthCheck
{
    private const int TimeoutComandoSegundos = 5;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = TimeoutComandoSegundos;
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("SQL Server disponible.");
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or OperationCanceledException)
        {
            // El detalle queda en la excepción para los logs; la respuesta HTTP no lo expone.
            return new HealthCheckResult(context.Registration.FailureStatus, "SQL Server no disponible.", ex);
        }
    }
}
