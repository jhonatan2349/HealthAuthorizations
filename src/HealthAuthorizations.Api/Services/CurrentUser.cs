using System.Security.Claims;
using HealthAuthorizations.Application.Common.Interfaces;

namespace HealthAuthorizations.Api.Services;

/// <summary>Obtiene el usuario autenticado desde el contexto HTTP (claims del JWT).</summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? UsuarioId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.Identity?.Name;
        }
    }
}
