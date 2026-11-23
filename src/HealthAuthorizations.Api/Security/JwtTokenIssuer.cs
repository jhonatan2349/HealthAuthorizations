using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HealthAuthorizations.Api.Security;

public sealed record TokenEmitido(string AccessToken, string TokenType, int ExpiresIn, DateTimeOffset ExpiraEn);

/// <summary>Emite JWT firmados con la clave simétrica configurada (emisor de demo y pruebas).</summary>
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    public TokenEmitido Emitir(string usuario, string nombre, IEnumerable<string> roles)
    {
        var jwt = options.Value;
        if (jwt.UsaAuthority)
        {
            throw new InvalidOperationException("Con un proveedor de identidad (Jwt:Authority) los tokens no se emiten en la API.");
        }

        var ahora = timeProvider.GetUtcNow();
        var expira = ahora.AddMinutes(jwt.MinutosVigencia);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = ahora.UtcDateTime,
            NotBefore = ahora.UtcDateTime,
            Expires = expira.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [Claims.Subject] = usuario,
                [Claims.Name] = nombre,
                [Claims.Role] = roles.ToArray(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey!)),
                SecurityAlgorithms.HmacSha256),
        };

        return new TokenEmitido(
            new JsonWebTokenHandler().CreateToken(descriptor),
            "Bearer",
            jwt.MinutosVigencia * 60,
            expira);
    }
}
