using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Catalogos;

/// <summary>
/// Base para catálogos identificados por un código de negocio único y un nombre.
/// Los catálogos no se eliminan: se desactivan para preservar la integridad histórica.
/// </summary>
public abstract class Catalogo : AuditableEntity<int>
{
    public const int MaxLongitudCodigo = 20;
    public const int MaxLongitudNombre = 150;

    protected Catalogo()
    {
    }

    protected Catalogo(string codigo, string nombre)
    {
        Codigo = Guard.Requerido(codigo, nameof(Codigo), MaxLongitudCodigo).ToUpperInvariant();
        Nombre = Guard.Requerido(nombre, nameof(Nombre), MaxLongitudNombre);
        Activo = true;
    }

    public string Codigo { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public bool Activo { get; private set; }

    public void Renombrar(string nombre) => Nombre = Guard.Requerido(nombre, nameof(Nombre), MaxLongitudNombre);

    public void Activar() => Activo = true;

    public void Desactivar() => Activo = false;
}
