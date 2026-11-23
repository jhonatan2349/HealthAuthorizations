using HealthAuthorizations.Application.Autorizaciones.Actualizar;
using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Autorizaciones.Inactivar;
using HealthAuthorizations.Application.Autorizaciones.Obtener;
using HealthAuthorizations.Application.Common.Exceptions;
using HealthAuthorizations.Application.Common.Results;
using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.IntegrationTests.Autorizaciones;

public sealed class ModificarAutorizacionTests(DatabaseFixture fixture)
{
    private async Task<AutorizacionDto> CrearAsync() => (await fixture.SendAsync(Datos.Crear())).Value.Autorizacion;

    private static ActualizarAutorizacionCommand Actualizar(AutorizacionDto a, string? version = null) =>
        new(a.Id, PrestadorId: 7, Datos.Hoy, "Control", "Ajustada", [new ServicioRequest(Datos.TipoImagenes, "Radiografía de tórax", 2000m)], version);

    [Fact]
    public async Task Actualizar_ReemplazaServiciosRecalculaYAudita()
    {
        var original = await CrearAsync();

        var result = await fixture.SendAsync(Actualizar(original, original.Version));

        var dto = result.Value;
        dto.Prestador.Id.ShouldBe(7);
        dto.Servicios.ShouldHaveSingleItem().TipoServicio.Codigo.ShouldBe("IMG");
        dto.MontoArs.ShouldBe(1400m);
        dto.MontoAfiliado.ShouldBe(600m);
        dto.ModificadoPor.ShouldBe(DatabaseFixture.UsuarioPruebas);
        dto.Version.ShouldNotBe(original.Version);

        // Los detalles reemplazados se eliminan (no quedan huérfanos).
        (await fixture.QueryAsync(db => db.Set<AutorizacionDetalle>().CountAsync(d => d.AutorizacionId == original.Id))).ShouldBe(1);
    }

    [Fact]
    public async Task Actualizar_ConVersionDesactualizada_LanzaConflictoDeConcurrencia()
    {
        var original = await CrearAsync();
        await fixture.SendAsync(Actualizar(original, original.Version));

        await Should.ThrowAsync<ConcurrencyConflictException>(() => fixture.SendAsync(Actualizar(original, original.Version)));
    }

    [Fact]
    public async Task Actualizar_Inexistente_DevuelveNoEncontrada()
    {
        var result = await fixture.SendAsync(Actualizar((await CrearAsync()) with { Id = 999_999_999 }));

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Actualizar_ConPrestadorInexistente_NoModifica()
    {
        var original = await CrearAsync();

        var result = await fixture.SendAsync(Actualizar(original) with { PrestadorId = 999_999 });

        result.Error!.Code.ShouldBe("Prestador.NoExiste");
        (await fixture.SendAsync(new ObtenerAutorizacionQuery(original.Id))).Value.Version.ShouldBe(original.Version);
    }

    [Fact]
    public async Task Inactivar_ConservaElRegistroYBloqueaModificaciones()
    {
        var original = await CrearAsync();

        var result = await fixture.SendAsync(new InactivarAutorizacionCommand(original.Id, "Emitida por error", original.Version));

        result.IsSuccess.ShouldBeTrue();
        var inactiva = (await fixture.SendAsync(new ObtenerAutorizacionQuery(original.Id))).Value;
        inactiva.Estado.ShouldBe(EstadoAutorizacion.Inactiva);
        inactiva.MotivoInactivacion.ShouldBe("Emitida por error");
        inactiva.FechaInactivacion.ShouldNotBeNull();
        inactiva.Servicios.Count.ShouldBe(original.Servicios.Count);

        (await fixture.SendAsync(Actualizar(original))).Error!.Code.ShouldBe("Autorizacion.Inactiva");
        (await fixture.SendAsync(new InactivarAutorizacionCommand(original.Id, "Otra vez"))).Error!.Code.ShouldBe("Autorizacion.Inactiva");
    }

    [Fact]
    public async Task Inactivar_ConVersionDesactualizada_LanzaConflictoDeConcurrencia()
    {
        var original = await CrearAsync();
        await fixture.SendAsync(Actualizar(original));

        await Should.ThrowAsync<ConcurrencyConflictException>(() =>
            fixture.SendAsync(new InactivarAutorizacionCommand(original.Id, "Motivo", original.Version)));
    }

    [Fact]
    public async Task Inactivar_Inexistente_DevuelveNoEncontrada() =>
        (await fixture.SendAsync(new InactivarAutorizacionCommand(999_999_999, "Motivo"))).Error!.Type.ShouldBe(ErrorType.NotFound);

    [Fact]
    public async Task Obtener_Inexistente_DevuelveNoEncontrada() =>
        (await fixture.SendAsync(new ObtenerAutorizacionQuery(999_999_999))).Error!.Code.ShouldBe("Autorizacion.NoEncontrada");
}
