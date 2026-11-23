using System.Net.Http.Headers;
using HealthAuthorizations.Api.Security;
using HealthAuthorizations.Application;
using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Domain.Catalogos;
using HealthAuthorizations.Infrastructure;
using HealthAuthorizations.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(HealthAuthorizations.IntegrationTests.Infrastructure.DatabaseFixture))]

namespace HealthAuthorizations.IntegrationTests.Infrastructure;

/// <summary>
/// SQL Server real en un contenedor efímero, compartido por todo el ensamblado y migrado una sola vez.
/// Las pruebas se aíslan por datos (cada una crea su propio afiliado cuando necesita aislamiento).
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    public const string UsuarioPruebas = "usuario-pruebas";

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private ServiceProvider? _services;
    private ApiFactory? _api;
    private int _secuencia;

    public IServiceProvider Services => _services ?? throw new InvalidOperationException("Fixture no inicializado.");

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _container.GetConnectionString(),
                ["Cobertura:PorcentajeArs"] = "70",
            })
            .Build();

        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<ICurrentUser>(new UsuarioFijo())
            .AddApplication(configuration)
            .AddInfrastructure(configuration)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await using var scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

        _api = new ApiFactory(_container.GetConnectionString());
    }

    public const string SujetoApi = "operador-pruebas";

    private ApiFactory Api => _api ?? throw new InvalidOperationException("Fixture no inicializado.");

    /// <summary>Cliente HTTP autenticado contra la API real en memoria (pipeline completo: middleware, MVC, filtros).</summary>
    public HttpClient CreateClient(string rol = Roles.Operador)
    {
        var token = Api.Services.GetRequiredService<JwtTokenIssuer>().Emitir(SujetoApi, "Usuario de pruebas", [rol]);
        var client = Api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    public HttpClient CreateAnonymousClient() => Api.CreateClient();

    /// <summary>API con configuración distinta (p. ej. límites de tasa bajos) sobre la misma base de datos.</summary>
    internal ApiFactory CreateApi(IReadOnlyDictionary<string, string?> ajustes) => new(_container.GetConnectionString(), ajustes);

    public async ValueTask DisposeAsync()
    {
        if (_api is not null)
        {
            await _api.DisposeAsync();
        }

        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    /// <summary>Envía la solicitud en su propio scope, como lo haría una petición HTTP.</summary>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    public async Task<T> QueryAsync<T>(Func<ApplicationDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    /// <summary>Crea un afiliado exclusivo para la prueba (aislamiento de datos en el listado).</summary>
    public async Task<int> CrearAfiliadoAsync(int contratanteId = 1, int? intermediarioId = 2, bool activo = true)
    {
        var n = Interlocked.Increment(ref _secuencia);
        var afiliado = new Afiliado($"T-{Guid.NewGuid():N}"[..20], $"T{n:D6}{Random.Shared.Next(1000, 9999)}", "Prueba", $"Integración {n}", new DateOnly(1990, 1, 1), contratanteId, intermediarioId);
        if (!activo)
        {
            afiliado.Desactivar();
        }

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Afiliados.Add(afiliado);
        await db.SaveChangesAsync();
        return afiliado.Id;
    }

    private sealed class UsuarioFijo : ICurrentUser
    {
        public string? UsuarioId => UsuarioPruebas;
    }
}
