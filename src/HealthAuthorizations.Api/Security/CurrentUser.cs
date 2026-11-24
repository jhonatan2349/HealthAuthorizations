using HealthAuthorizations.Application.Common.Interfaces;

namespace HealthAuthorizations.Api.Security;

/// <summary>Usuario autenticado según el claim "sub" del JWT; se usa para la auditoría.</summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? UsuarioId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(Claims.Subject)?.Value ?? user?.Identity?.Name;
        }
    }
}
