namespace HealthAuthorizations.Application.Autorizaciones.Common;

/// <summary>Datos comunes a crear y actualizar; permite compartir validaciones sin duplicarlas.</summary>
public interface IDatosAutorizacion
{
    int PrestadorId { get; }

    DateOnly FechaServicio { get; }

    string? Diagnostico { get; }

    string? Observaciones { get; }

    IReadOnlyList<ServicioRequest> Servicios { get; }
}
