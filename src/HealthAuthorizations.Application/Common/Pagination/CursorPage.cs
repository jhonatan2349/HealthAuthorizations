namespace HealthAuthorizations.Application.Common.Pagination;

/// <summary>
/// Página de resultados con paginación por cursor (keyset).
/// No incluye el total: un COUNT(*) sobre millones de filas costaría más que la propia consulta.
/// </summary>
public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor, int Limit)
{
    public bool HasMore => NextCursor is not null;
}
