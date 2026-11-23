namespace HealthAuthorizations.Api.Contracts;

/// <summary>Credenciales de un usuario de demostración.</summary>
public sealed record TokenRequest(string Usuario, string Clave);
