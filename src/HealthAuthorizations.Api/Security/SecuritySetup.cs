using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HealthAuthorizations.Api.Security;

internal static class SecuritySetup
{
    /// <summary>Tolerancia de reloj entre servidores; el valor por defecto (5 min) alarga de más la vida de un token.</summary>
    private static readonly TimeSpan ToleranciaReloj = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddApiSecurity(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(o => o.EsValida, $"Configuración Jwt inválida: Issuer, Audience y Authority o SigningKey (mínimo {JwtOptions.MinLongitudClaveBytes} bytes) son obligatorios.")
            .ValidateOnStart();

        services.AddOptions<EmisorDemoOptions>().BindConfiguration(EmisorDemoOptions.SectionName);
        services.AddSingleton<JwtTokenIssuer>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Se configura con IOptions para leer la configuración ya resuelta (incluye user-secrets y variables de entorno).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = ToleranciaReloj,
                    NameClaimType = Claims.Name,
                    RoleClaimType = Claims.Role,
                };

                if (jwt.UsaAuthority)
                {
                    bearer.Authority = jwt.Authority;
                }
                else
                {
                    bearer.TokenValidationParameters.ValidateIssuerSigningKey = true;
                    bearer.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey!));
                    bearer.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.HmacSha256];
                }
            });

        services.AddAuthorizationBuilder()
            // Todo endpoint exige autenticación salvo los marcados explícitamente con [AllowAnonymous].
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Politicas.Lectura, p => p.RequireAuthenticatedUser().RequireRole(Roles.Lector, Roles.Operador))
            .AddPolicy(Politicas.Escritura, p => p.RequireAuthenticatedUser().RequireRole(Roles.Operador));

        return services;
    }
}
