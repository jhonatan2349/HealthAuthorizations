using System.Text;

namespace HealthAuthorizations.Api.Security;

/// <summary>
/// Configuración de JWT (sección "Jwt").
/// Con <see cref="Authority"/> se validan tokens de un proveedor OIDC (Entra ID, Keycloak) usando sus claves públicas.
/// Sin él, se usa <see cref="SigningKey"/> (HMAC-SHA256), pensado para desarrollo y la demo.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinLongitudClaveBytes = 32;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    /// <summary>URL del proveedor de identidad (OIDC discovery). Opcional.</summary>
    public string? Authority { get; init; }

    /// <summary>Secreto simétrico; nunca en el repositorio (user-secrets, variables de entorno, K8s Secret).</summary>
    public string? SigningKey { get; init; }

    public int MinutosVigencia { get; init; } = 60;

    public bool UsaAuthority => !string.IsNullOrWhiteSpace(Authority);

    public bool EsValida =>
        !string.IsNullOrWhiteSpace(Issuer)
        && !string.IsNullOrWhiteSpace(Audience)
        && MinutosVigencia is > 0 and <= 1440
        && (UsaAuthority || Encoding.UTF8.GetByteCount(SigningKey ?? string.Empty) >= MinLongitudClaveBytes);
}
