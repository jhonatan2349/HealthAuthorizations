namespace HealthAuthorizations.Domain.Common;

/// <summary>Validaciones reutilizables para proteger las invariantes de las entidades.</summary>
public static class Guard
{
    public static string Requerido(string? valor, string campo, int maxLongitud)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException($"El campo '{campo}' es obligatorio.");
        }

        return Longitud(valor.Trim(), campo, maxLongitud)!;
    }

    public static string? Opcional(string? valor, string campo, int maxLongitud) =>
        string.IsNullOrWhiteSpace(valor) ? null : Longitud(valor.Trim(), campo, maxLongitud);

    public static int IdValido(int id, string campo) =>
        id > 0 ? id : throw new DomainException($"El campo '{campo}' debe ser un identificador válido.");

    private static string? Longitud(string valor, string campo, int maxLongitud) =>
        valor.Length <= maxLongitud
            ? valor
            : throw new DomainException($"El campo '{campo}' no puede superar {maxLongitud} caracteres.");
}
