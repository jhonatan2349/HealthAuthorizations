using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HealthAuthorizations.IntegrationTests.Infrastructure;

/// <summary>Levanta la API real apuntando al SQL Server del contenedor, con una clave JWT efímera.</summary>
internal sealed class ApiFactory(string connectionString, IReadOnlyDictionary<string, string?>? ajustes = null) : WebApplicationFactory<Program>
{
    public const string UsuarioDemo = "operador-demo";
    public const string ClaveDemo = "Clave.De.Prueba.123";

    private static readonly string ClaveFirma = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" no carga user-secrets, así la configuración del contenedor no se pisa.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Jwt:SigningKey", ClaveFirma);
        builder.UseSetting("Auth:EmisorDemo:Habilitado", "true");
        builder.UseSetting("Auth:EmisorDemo:Usuarios:0:Usuario", UsuarioDemo);
        builder.UseSetting("Auth:EmisorDemo:Usuarios:0:Nombre", "Operador de pruebas");
        builder.UseSetting("Auth:EmisorDemo:Usuarios:0:Clave", ClaveDemo);
        builder.UseSetting("Auth:EmisorDemo:Usuarios:0:Roles:0", "autorizaciones.operador");

        foreach (var (clave, valor) in ajustes ?? new Dictionary<string, string?>())
        {
            builder.UseSetting(clave, valor);
        }
    }
}
