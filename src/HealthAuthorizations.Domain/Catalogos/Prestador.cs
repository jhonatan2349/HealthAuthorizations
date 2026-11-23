using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Catalogos;

/// <summary>Proveedor que presta el servicio de salud autorizado.</summary>
public sealed class Prestador : Catalogo
{
    public const int MaxLongitudDocumento = 15;

    private Prestador()
    {
    }

    public Prestador(string codigo, string nombre, string documento, int tipoPrestadorId)
        : base(codigo, nombre)
    {
        Documento = Guard.Requerido(documento, nameof(Documento), MaxLongitudDocumento);
        TipoPrestadorId = Guard.IdValido(tipoPrestadorId, nameof(TipoPrestadorId));
    }

    /// <summary>RNC o cédula/exequátur del prestador.</summary>
    public string Documento { get; private set; } = string.Empty;

    public int TipoPrestadorId { get; private set; }

    public TipoPrestador? TipoPrestador { get; private set; }
}
