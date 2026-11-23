using HealthAuthorizations.Domain.Cobertura;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.UnitTests.Domain;

public sealed class PoliticaCoberturaPorcentualTests
{
    [Fact]
    public void PorDefecto_LaArsCubre70PorCiento()
    {
        var politica = new PoliticaCoberturaPorcentual();

        var distribucion = politica.Distribuir(tipoServicioId: 1, montoReclamado: 1000m);

        distribucion.MontoArs.ShouldBe(700m);
        distribucion.MontoAfiliado.ShouldBe(300m);
    }

    [Fact]
    public void ConPorcentajeConfigurado_AplicaLaNuevaRegla()
    {
        var politica = new PoliticaCoberturaPorcentual(80m);

        var distribucion = politica.Distribuir(tipoServicioId: 1, montoReclamado: 1000m);

        distribucion.MontoArs.ShouldBe(800m);
        distribucion.MontoAfiliado.ShouldBe(200m);
        distribucion.PorcentajeArs.ShouldBe(80m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ConPorcentajeInvalido_FallaAlConstruir(decimal porcentaje) =>
        Should.Throw<DomainException>(() => new PoliticaCoberturaPorcentual(porcentaje));
}
