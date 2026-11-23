using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HealthAuthorizations.IntegrationTests.Infrastructure;

/// <summary>Levanta la API real apuntando al SQL Server del contenedor.</summary>
internal sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" no carga user-secrets, así la cadena del contenedor no se pisa.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
    }
}
