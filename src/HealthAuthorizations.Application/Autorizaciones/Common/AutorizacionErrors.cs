using HealthAuthorizations.Application.Common.Results;

namespace HealthAuthorizations.Application.Autorizaciones.Common;

/// <summary>Catálogo de errores esperados del módulo, con códigos estables para los clientes.</summary>
public static class AutorizacionErrors
{
    public static Error NoEncontrada(long id) =>
        Error.NotFound("Autorizacion.NoEncontrada", $"No existe la autorización {id}.");

    public static Error Inactiva(long id) =>
        Error.Conflict("Autorizacion.Inactiva", $"La autorización {id} está inactiva y no puede modificarse.");

    public static Error ClaveIdempotenciaEnUso() =>
        Error.Conflict(
            "Autorizacion.ClaveIdempotenciaEnUso",
            "La clave de idempotencia ya se utilizó para una autorización de otro afiliado.");

    public static Error AfiliadoNoExiste(int id) =>
        Error.BusinessRule("Afiliado.NoExiste", $"No existe el afiliado {id}.");

    public static Error AfiliadoInactivo(int id) =>
        Error.BusinessRule("Afiliado.Inactivo", $"El afiliado {id} está inactivo.");

    public static Error ContratanteInactivo(int id) =>
        Error.BusinessRule("Contratante.Inactivo", $"El contratante {id} del afiliado está inactivo.");

    public static Error PrestadorNoExiste(int id) =>
        Error.BusinessRule("Prestador.NoExiste", $"No existe el prestador {id}.");

    public static Error PrestadorInactivo(int id) =>
        Error.BusinessRule("Prestador.Inactivo", $"El prestador {id} está inactivo.");

    public static Error TiposServicioNoExisten(IEnumerable<int> ids) =>
        Error.BusinessRule("TipoServicio.NoExiste", $"No existen o están inactivos los tipos de servicio: {string.Join(", ", ids)}.");
}
