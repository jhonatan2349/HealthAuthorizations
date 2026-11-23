using System.Security.Cryptography;
using System.Text;
using HealthAuthorizations.Api.Contracts;
using HealthAuthorizations.Api.ErrorHandling;
using HealthAuthorizations.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace HealthAuthorizations.Api.Controllers;

/// <summary>Emisión de tokens para la demo. En producción el token lo emite el proveedor de identidad.</summary>
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController(
    ISender sender,
    IOptions<EmisorDemoOptions> emisorOptions,
    JwtTokenIssuer issuer) : ApiControllerBase(sender)
{
    /// <summary>Obtiene un token de acceso con un usuario de demostración.</summary>
    /// <remarks>Solo disponible cuando <c>Auth:EmisorDemo:Habilitado</c> es true (desarrollo y demo).</remarks>
    [HttpPost("token")]
    [EnableRateLimiting(RateLimitingSetup.PoliticaToken)]
    [ProducesResponseType<TokenEmitido>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public IActionResult Token([FromBody] TokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var emisor = emisorOptions.Value;
        if (!emisor.Habilitado)
        {
            // Se responde 404 para no revelar que el endpoint existe.
            return NotFound();
        }

        var usuario = emisor.Usuarios.FirstOrDefault(u => string.Equals(u.Usuario, request.Usuario, StringComparison.Ordinal));

        // Se compara siempre (incluso si el usuario no existe) y en tiempo constante, para no filtrar información por tiempos.
        var claveValida = ClavesIguales(usuario?.Clave ?? string.Empty, request.Clave ?? string.Empty);

        if (usuario is null || !claveValida)
        {
            var problem = ProblemDetailsFactory.CreateProblemDetails(
                HttpContext, StatusCodes.Status401Unauthorized, "Credenciales inválidas", detail: "Usuario o clave incorrectos.");
            problem.Extensions[ProblemTitles.ExtensionCodigo] = "Autenticacion.CredencialesInvalidas";
            return new ObjectResult(problem) { StatusCode = StatusCodes.Status401Unauthorized };
        }

        return Ok(issuer.Emitir(usuario.Usuario, usuario.Nombre, usuario.Roles));
    }

    private static bool ClavesIguales(string esperada, string recibida) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(esperada)),
            SHA256.HashData(Encoding.UTF8.GetBytes(recibida)));
}
