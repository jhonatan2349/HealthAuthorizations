using System.Text.Json.Serialization;
using HealthAuthorizations.Api.ErrorHandling;
using HealthAuthorizations.Api.Middleware;
using HealthAuthorizations.Api.OpenApi;
using HealthAuthorizations.Api.Security;
using HealthAuthorizations.Api.Services;
using HealthAuthorizations.Application;
using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddApiDocumentation();
builder.Services.AddApiProblemDetails();
builder.Services.AddApiSecurity();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

// La documentación es un entregable: se publica en todos los ambientes salvo producción.
if (!app.Environment.IsProduction())
{
    app.MapApiDocumentation();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

/// <summary>Punto de entrada; parcial y público para las pruebas con WebApplicationFactory.</summary>
public partial class Program;
