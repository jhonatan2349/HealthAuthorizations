using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Cobertura;

/// <summary>
/// Reparto de un monto reclamado entre la ARS y el afiliado.
/// Invariante: <c>MontoArs + MontoAfiliado == MontoReclamado</c>, sin negativos.
/// </summary>
public sealed record DistribucionMonto
{
    public const int Decimales = 2;

    private DistribucionMonto(decimal montoReclamado, decimal porcentajeArs, decimal montoArs, decimal montoAfiliado)
    {
        MontoReclamado = montoReclamado;
        PorcentajeArs = porcentajeArs;
        MontoArs = montoArs;
        MontoAfiliado = montoAfiliado;
    }

    public decimal MontoReclamado { get; }

    /// <summary>Porcentaje (0–100) cubierto por la ARS. Se guarda con cada detalle para conservar el histórico.</summary>
    public decimal PorcentajeArs { get; }

    public decimal MontoArs { get; }

    public decimal MontoAfiliado { get; }

    /// <summary>
    /// Calcula la distribución. La parte de la ARS se redondea a 2 decimales (half away from zero)
    /// y la del afiliado se obtiene por diferencia, de modo que la suma siempre cuadra al centavo.
    /// </summary>
    public static DistribucionMonto PorPorcentaje(decimal montoReclamado, decimal porcentajeArs)
    {
        ValidarMonto(montoReclamado);

        ValidarPorcentaje(porcentajeArs);

        var montoArs = Math.Round(montoReclamado * porcentajeArs / 100m, Decimales, MidpointRounding.AwayFromZero);

        return new DistribucionMonto(montoReclamado, porcentajeArs, montoArs, montoReclamado - montoArs);
    }

    internal static void ValidarPorcentaje(decimal porcentaje)
    {
        if (porcentaje is < 0 or > 100)
        {
            throw new DomainException("El porcentaje de cobertura debe estar entre 0 y 100.");
        }
    }

    private static void ValidarMonto(decimal monto)
    {
        if (monto <= 0)
        {
            throw new DomainException("El monto reclamado debe ser mayor que cero.");
        }

        if (decimal.Round(monto, Decimales) != monto)
        {
            throw new DomainException($"El monto reclamado admite como máximo {Decimales} decimales.");
        }
    }
}
