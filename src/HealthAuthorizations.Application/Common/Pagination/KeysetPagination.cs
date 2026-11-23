using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Application.Common.Pagination;

/// <summary>Contrato común de las consultas paginadas por cursor.</summary>
public interface IPaginada
{
    string? Cursor { get; }

    int Limite { get; }
}

public static class KeysetPagination
{
    /// <summary>
    /// Materializa una página a partir de una consulta ya filtrada por cursor y ordenada por la clave.
    /// Pide una fila de más para saber si existe otra página sin ejecutar COUNT(*).
    /// </summary>
    public static async Task<CursorPage<T>> ToCursorPageAsync<T>(
        this IQueryable<T> consultaOrdenada,
        int limite,
        Func<T, long> clave,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(clave);

        var filas = await consultaOrdenada.Take(limite + 1).ToListAsync(cancellationToken);

        if (filas.Count <= limite)
        {
            return new CursorPage<T>(filas, null, limite);
        }

        filas.RemoveAt(filas.Count - 1);
        return new CursorPage<T>(filas, Cursor.Encode(clave(filas[^1])), limite);
    }
}
