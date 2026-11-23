namespace HealthAuthorizations.Api.Security;

/// <summary>
/// Emisor de tokens SOLO para desarrollo, pruebas y la demo (sección "Auth:EmisorDemo").
/// En producción los tokens los emite el proveedor de identidad y este emisor no puede habilitarse.
/// </summary>
public sealed class EmisorDemoOptions
{
    public const string SectionName = "Auth:EmisorDemo";

    public bool Habilitado { get; init; }

    public IReadOnlyList<UsuarioDemo> Usuarios { get; init; } = [];
}

public sealed class UsuarioDemo
{
    public string Usuario { get; init; } = string.Empty;

    public string Nombre { get; init; } = string.Empty;

    public string Clave { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];
}
