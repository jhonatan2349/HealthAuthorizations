namespace HealthAuthorizations.Application.Common.Interfaces;

/// <summary>Usuario que ejecuta la operación (a partir del token JWT en la Fase 5).</summary>
public interface ICurrentUser
{
    string? UsuarioId { get; }
}
