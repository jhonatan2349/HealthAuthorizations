using HealthAuthorizations.Domain.Cobertura;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.UnitTests.Domain;

public sealed class DistribucionMontoTests
{
    [Theory]
    [InlineData(1000.00, 700.00, 300.00)]
    [InlineData(100.00, 70.00, 30.00)]
    [InlineData(0.01, 0.01, 0.00)]   // 0.007 -> 0.01 (half away from zero)
    [InlineData(0.05, 0.04, 0.01)]   // 0.035 -> 0.04
    [InlineData(333.33, 233.33, 100.00)]
    [InlineData(1234.57, 864.20, 370.37)]
    public void PorPorcentaje_Con70_DistribuyeYRedondeaCorrectamente(decimal reclamado, decimal ars, decimal afiliado)
    {
        var distribucion = DistribucionMonto.PorPorcentaje(reclamado, 70m);

        distribucion.MontoArs.ShouldBe(ars);
        distribucion.MontoAfiliado.ShouldBe(afiliado);
        distribucion.PorcentajeArs.ShouldBe(70m);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.05)]
    [InlineData(10.03)]
    [InlineData(99999.99)]
    [InlineData(1234567.89)]
    public void PorPorcentaje_LaSumaSiempreCuadraAlCentavo(decimal reclamado)
    {
        var distribucion = DistribucionMonto.PorPorcentaje(reclamado, 70m);

        (distribucion.MontoArs + distribucion.MontoAfiliado).ShouldBe(reclamado);
        distribucion.MontoArs.ShouldBeGreaterThanOrEqualTo(0m);
        distribucion.MontoAfiliado.ShouldBeGreaterThanOrEqualTo(0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void PorPorcentaje_EnLosExtremos_AsignaTodoAUnaParte(decimal porcentaje)
    {
        var distribucion = DistribucionMonto.PorPorcentaje(500m, porcentaje);

        distribucion.MontoArs.ShouldBe(500m * porcentaje / 100m);
        distribucion.MontoAfiliado.ShouldBe(500m - distribucion.MontoArs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void PorPorcentaje_ConMontoNoPositivo_Lanza(decimal reclamado) =>
        Should.Throw<DomainException>(() => DistribucionMonto.PorPorcentaje(reclamado, 70m));

    [Fact]
    public void PorPorcentaje_ConMasDeDosDecimales_Lanza() =>
        Should.Throw<DomainException>(() => DistribucionMonto.PorPorcentaje(10.001m, 70m));

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void PorPorcentaje_ConPorcentajeFueraDeRango_Lanza(decimal porcentaje) =>
        Should.Throw<DomainException>(() => DistribucionMonto.PorPorcentaje(100m, porcentaje));
}
