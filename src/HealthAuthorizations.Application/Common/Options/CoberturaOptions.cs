namespace HealthAuthorizations.Application.Common.Options;

/// <summary>
/// Configuración de la regla de distribución de montos (sección "Cobertura").
/// Cambiar el porcentaje no requiere modificar código; solo configuración.
/// </summary>
public sealed class CoberturaOptions
{
    public const string SectionName = "Cobertura";

    public decimal PorcentajeArs { get; init; } = 70m;

    public bool EsValida => PorcentajeArs is >= 0 and <= 100;
}
