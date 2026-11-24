using System.Text.Json.Serialization;
using HealthAuthorizations.Api.ErrorHandling;
using HealthAuthorizations.Api.Hosting;
using HealthAuthorizations.Api.Middleware;
using HealthAuthorizations.Api.Observability;
using HealthAuthorizations.Api.OpenApi;
using HealthAuthorizations.Api.Security;
using HealthAuthorizations.Application;
using HealthAuthorizations.Application.Common.Interfaces;
using HealthAuthorizations.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Una autorización con 50 servicios ocupa unos pocos KB: 1 MB de margen limita abusos.
const long MaxRequestBodyBytes = 1024 * 1024;

builder.WebHost.ConfigureKestrel(kestrel =>
{
    // No revelar la tecnología del servidor.
    kestrel.AddServerHeader = false;
    kestrel.Limits.MaxRequestBodySize = MaxRequestBodyBytes;
});

builder.AddApiObservability();
builder.AddApiResilience();

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add(new RequestSizeLimitAttribute(MaxRequestBodyBytes));

        // La validación vive en FluentValidation (Application): se evita la validación implícita de MVC
        // para tipos no anulables, que duplicaba reglas y respondía en inglés.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddApiDocumentation();
builder.Services.AddApiProblemDetails();
builder.Services.AddApiSecurity();
builder.Services.AddApiRateLimiting();
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddApiProxyInverso(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseApiProxyInverso();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

// La documentación es un entregable: se publica en todos los ambientes salvo producción.
if (!app.Environment.IsProduction())
{
    app.MapApiDocumentation();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// En contenedor TLS termina en el ingress y Kestrel solo escucha HTTP: la imagen desactiva la redirección.
if (app.Configuration.GetValue("Https:RedireccionHabilitada", defaultValue: true))
{
    app.UseHttpsRedirection();
}

app.UseCors();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseRequestTimeouts();

app.MapControllers();
app.MapApiHealthChecks();

await app.RunAsync();

/// <summary>Punto de entrada; parcial y público para las pruebas con WebApplicationFactory.</summary>
public partial class Program
{
    protected Program()
    {
    }
}
