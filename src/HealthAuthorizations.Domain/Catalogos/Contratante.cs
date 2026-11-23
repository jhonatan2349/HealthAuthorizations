using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Catalogos;

/// <summary>Cliente (empresa o persona) que contrata el plan de salud de los afiliados.</summary>
public sealed class Contratante : Catalogo
{
    public const int MaxLongitudDocumento = 15;

    private Contratante()
    {
    }

    public Contratante(string codigo, string nombre, string documento)
        : base(codigo, nombre)
    {
        Documento = Guard.Requerido(documento, nameof(Documento), MaxLongitudDocumento);
    }

    /// <summary>RNC o cédula del contratante.</summary>
    public string Documento { get; private set; } = string.Empty;
}
