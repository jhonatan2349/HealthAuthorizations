using HealthAuthorizations.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.IntegrationTests.Autorizaciones;

public sealed class IdempotenciaTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Crear_DosVecesConLaMismaClave_DevuelveLaMismaAutorizacion()
    {
        var clave = Guid.NewGuid().ToString();

        var primera = await fixture.SendAsync(Datos.Crear(clave: clave));
        var segunda = await fixture.SendAsync(Datos.Crear(clave: clave));

        segunda.Value.EsReintento.ShouldBeTrue();
        segunda.Value.Autorizacion.Id.ShouldBe(primera.Value.Autorizacion.Id);
        (await ContarAsync(clave)).ShouldBe(1);
    }

    [Fact]
    public async Task Crear_SolicitudesSimultaneasConLaMismaClave_CreaUnaSola()
    {
        var clave = Guid.NewGuid().ToString();

        var resultados = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => fixture.SendAsync(Datos.Crear(clave: clave))));

        resultados.ShouldAllBe(r => r.IsSuccess);
        resultados.Select(r => r.Value.Autorizacion.Id).Distinct().ShouldHaveSingleItem();
        resultados.Count(r => !r.Value.EsReintento).ShouldBe(1);
        (await ContarAsync(clave)).ShouldBe(1);
    }

    [Fact]
    public async Task Crear_ConClaveUsadaPorOtroAfiliado_DevuelveConflicto()
    {
        var clave = Guid.NewGuid().ToString();
        var otroAfiliado = await fixture.CrearAfiliadoAsync();
        await fixture.SendAsync(Datos.Crear(clave: clave));

        var result = await fixture.SendAsync(Datos.Crear(otroAfiliado, clave));

        result.Error!.Code.ShouldBe("Autorizacion.ClaveIdempotenciaEnUso");
    }

    private Task<int> ContarAsync(string clave) =>
        fixture.QueryAsync(db => db.Autorizaciones.CountAsync(a => a.ClaveIdempotencia == clave));
}
