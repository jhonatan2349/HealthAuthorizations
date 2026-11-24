using FluentValidation;
using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.IntegrationTests.Autorizaciones;

public sealed class CrearAutorizacionTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Crear_ConDatosValidos_PersisteConMontosParticipantesYAuditoria()
    {
        var result = await fixture.SendAsync(Datos.Crear());

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value.Autorizacion;
        result.Value.EsReintento.ShouldBeFalse();

        dto.Numero.ShouldStartWith("AUT-");
        dto.Estado.ShouldBe(EstadoAutorizacion.Activa);
        dto.MontoReclamado.ShouldBe(1833.33m);
        dto.MontoArs.ShouldBe(1283.33m);
        dto.MontoAfiliado.ShouldBe(550.00m);
        dto.Servicios.Count.ShouldBe(2);
        dto.Servicios.ShouldAllBe(s => s.PorcentajeCobertura == 70m && s.MontoArs + s.MontoAfiliado == s.MontoReclamado);

        // Snapshot tomado del afiliado del seed.
        dto.Afiliado.Id.ShouldBe(Datos.AfiliadoSeed);
        dto.Contratante.Id.ShouldBe(1);
        dto.Intermediario!.Id.ShouldBe(1);
        dto.Prestador.Nombre.ShouldBe("Clínica Central");

        dto.CreadoPor.ShouldBe(DatabaseFixture.UsuarioPruebas);
        dto.Version.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Crear_AfiliadoSinIntermediario_DejaIntermediarioNulo()
    {
        var afiliado = await fixture.CrearAfiliadoAsync(intermediarioId: null);

        var result = await fixture.SendAsync(Datos.Crear(afiliado));

        result.Value.Autorizacion.Intermediario.ShouldBeNull();
    }

    [Fact]
    public async Task Crear_ConAfiliadoInexistente_DevuelveErrorDeNegocio()
    {
        var result = await fixture.SendAsync(Datos.Crear(afiliadoId: 999_999));

        result.Error!.Code.ShouldBe("Afiliado.NoExiste");
    }

    [Fact]
    public async Task Crear_ConAfiliadoInactivo_DevuelveErrorDeNegocio()
    {
        var afiliado = await fixture.CrearAfiliadoAsync(activo: false);

        var result = await fixture.SendAsync(Datos.Crear(afiliado));

        result.Error!.Code.ShouldBe("Afiliado.Inactivo");
    }

    [Fact]
    public async Task Crear_ConPrestadorInexistente_DevuelveErrorDeNegocio()
    {
        var result = await fixture.SendAsync(Datos.Crear() with { PrestadorId = 999_999 });

        result.Error!.Code.ShouldBe("Prestador.NoExiste");
    }

    [Fact]
    public async Task Crear_ConTiposDeServicioInexistentes_IndicaCuales()
    {
        var result = await fixture.SendAsync(Datos.Crear(servicios: [new ServicioRequest(998, "X", 10m), new ServicioRequest(999, "Y", 10m), new ServicioRequest(Datos.TipoConsulta, "Z", 10m)]));

        result.Error!.Code.ShouldBe("TipoServicio.NoExiste");
        result.Error.Message.ShouldContain("998, 999");
    }

    [Fact]
    public async Task Crear_ConDatosInvalidos_LanzaValidacionSinPersistir()
    {
        var afiliado = await fixture.CrearAfiliadoAsync();

        await Should.ThrowAsync<ValidationException>(() => fixture.SendAsync(Datos.Crear(afiliado) with { Servicios = [] }));

        (await fixture.QueryAsync(db => db.Autorizaciones.CountAsync(a => a.AfiliadoId == afiliado))).ShouldBe(0);
    }
}
