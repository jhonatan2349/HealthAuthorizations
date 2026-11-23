namespace HealthAuthorizations.Application.Autorizaciones.Common;

/// <summary>Representación textual del rowversion para usarlo como ETag / If-Match.</summary>
public static class VersionToken
{
    private const int LongitudRowVersion = 8;

    public static string Encode(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    public static bool TryDecode(string? token, out byte[] rowVersion)
    {
        rowVersion = [];
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var buffer = new byte[LongitudRowVersion];
        if (!Convert.TryFromBase64String(token, buffer, out var escritos) || escritos != LongitudRowVersion)
        {
            return false;
        }

        rowVersion = buffer;
        return true;
    }
}
