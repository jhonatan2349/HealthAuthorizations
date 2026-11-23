namespace HealthAuthorizations.Domain.Cobertura;

/// <summary>
/// Política vigente: la ARS cubre un porcentaje fijo (70 % por defecto) y el afiliado el resto.
/// El porcentaje se inyecta desde configuración.
/// </summary>
public sealed class PoliticaCoberturaPorcentual : IPoliticaCobertura
{
    public const decimal PorcentajeArsPorDefecto = 70m;

    private readonly decimal _porcentajeArs;

    public PoliticaCoberturaPorcentual(decimal porcentajeArs = PorcentajeArsPorDefecto)
    {
        DistribucionMonto.ValidarPorcentaje(porcentajeArs);
        _porcentajeArs = porcentajeArs;
    }

    public DistribucionMonto Distribuir(int tipoServicioId, decimal montoReclamado) =>
        DistribucionMonto.PorPorcentaje(montoReclamado, _porcentajeArs);
}
