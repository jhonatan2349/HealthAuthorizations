namespace HealthAuthorizations.Api.Http;

/// <summary>Conversión entre la versión (rowversion) y los headers ETag / If-Match / If-None-Match.</summary>
internal static class EntityTagHeader
{
    public static string Formatear(string version) => $"\"{version}\"";

    /// <summary>Extrae la versión de un header condicional. "*" o vacío significa sin precondición.</summary>
    public static string? Leer(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        var valor = header.Split(',')[0].Trim();
        if (valor.StartsWith("W/", StringComparison.Ordinal))
        {
            valor = valor[2..];
        }

        valor = valor.Trim('"');
        return valor is "" or "*" ? null : valor;
    }
}
