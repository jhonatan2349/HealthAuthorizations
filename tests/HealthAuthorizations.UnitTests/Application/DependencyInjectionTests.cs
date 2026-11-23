using HealthAuthorizations.Application;
using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.Domain.Cobertura;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthAuthorizations.UnitTests.Application;

public sealed class DependencyInjectionTests
{
    private static ServiceProvider Construir(decimal? porcentaje = null)
    {
        var valores = new Dictionary<string, string?>();
        if (porcentaje is not null)
        {
            valores["Cobertura:PorcentajeArs"] = porcentaje.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        return new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplication(configuration)
            .BuildServiceProvider();
    }

    [Fact]
    public void SinConfiguracion_UsaCobertura70PorCiento()
    {
        using var provider = Construir();

        provider.GetRequiredService<IPoliticaCobertura>().Distribuir(1, 100m).MontoArs.ShouldBe(70m);
    }

    [Fact]
    public void ConPorcentajeConfigurado_CambiaLaReglaSinTocarCodigo()
    {
        using var provider = Construir(80m);

        provider.GetRequiredService<IPoliticaCobertura>().Distribuir(1, 100m).MontoArs.ShouldBe(80m);
    }

    [Fact]
    public void ConPorcentajeInvalido_FallaLaValidacionDeOpciones()
    {
        using var provider = Construir(150m);

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IPoliticaCobertura>());
    }

    [Fact]
    public void Mapster_MapeaServiciosDeEntradaAlDominio()
    {
        using var provider = Construir();
        var mapper = provider.GetRequiredService<IMapper>();

        var servicios = mapper.Map<List<ServicioSolicitado>>(new List<ServicioRequest> { new(4, "Hemograma", 350.50m) });

        servicios.ShouldHaveSingleItem().ShouldBe(new ServicioSolicitado(4, "Hemograma", 350.50m));
    }
}
