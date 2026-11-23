using HealthAuthorizations.Domain.Catalogos;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.UnitTests.Domain;

public sealed class CatalogoTests
{
    [Fact]
    public void Crear_NormalizaCodigoYQuedaActivo()
    {
        var tipo = new TipoServicio(" con ", " Consulta ");

        tipo.Codigo.ShouldBe("CON");
        tipo.Nombre.ShouldBe("Consulta");
        tipo.Activo.ShouldBeTrue();
    }

    [Fact]
    public void Desactivar_YActivar_CambianElEstado()
    {
        var tipo = new TipoPrestador("MED", "Médico");

        tipo.Desactivar();
        tipo.Activo.ShouldBeFalse();

        tipo.Activar();
        tipo.Activo.ShouldBeTrue();
    }

    [Fact]
    public void Crear_ConCodigoDemasiadoLargo_Lanza() =>
        Should.Throw<DomainException>(() => new Intermediario(new string('X', Catalogo.MaxLongitudCodigo + 1), "Corredor"));

    [Fact]
    public void Prestador_SinTipoValido_Lanza() =>
        Should.Throw<DomainException>(() => new Prestador("P001", "Clínica", "101000001", tipoPrestadorId: 0));

    [Fact]
    public void Afiliado_ConIntermediarioOpcional_SeCreaActivo()
    {
        var afiliado = new Afiliado("AF-001", "00100000001", "Ana", "Pérez", new DateOnly(1990, 5, 1), contratanteId: 1, intermediarioId: null);

        afiliado.IntermediarioId.ShouldBeNull();
        afiliado.Activo.ShouldBeTrue();
    }

    [Fact]
    public void Afiliado_SinNombres_Lanza() =>
        Should.Throw<DomainException>(() => new Afiliado("AF-001", "001", "", "Pérez", new DateOnly(1990, 5, 1), 1, null));
}
