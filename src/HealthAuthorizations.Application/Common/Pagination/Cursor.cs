using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace HealthAuthorizations.Application.Common.Pagination;

/// <summary>
/// Cursor opaco para el cliente (base64url). Ocultar su contenido permite cambiar
/// la estrategia de ordenamiento sin romper el contrato de la API.
/// </summary>
public static class Cursor
{
    public static string Encode(long lastId) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes(lastId.ToString(CultureInfo.InvariantCulture)));

    public static bool TryDecode(string? cursor, out long lastId)
    {
        lastId = 0;
        if (string.IsNullOrWhiteSpace(cursor) || !Base64Url.IsValid(cursor))
        {
            return false;
        }

        var texto = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(cursor));
        return long.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out lastId) && lastId > 0;
    }
}
