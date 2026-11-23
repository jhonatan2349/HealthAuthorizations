using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Autorizaciones.Inactivar;
using HealthAuthorizations.Application.Autorizaciones.Listar;
using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.IntegrationTests.Infrastructure;

namespace HealthAuthorizations.IntegrationTests.Autorizaciones;

public sealed class ListarAutorizacionesTests(DatabaseFixture fixture)
{
    private async Task<(int AfiliadoId, List<long> Ids)> SembrarAsync(int cantidad)
    {
        var afiliado = await fixture.CrearAfiliadoAsync(contratanteId: 3);
        var ids = new List<long>();
        for (var i = 0; i < cantidad; i++)
        {
            var servicio = i % 2 == 0
                ? new ServicioRequest(Datos.TipoConsulta, "Consulta", 100m + i)
                : new ServicioRequest(Datos.TipoLaboratorio, "Laboratorio", 100m + i);
            var result = await fixture.SendAsync(Datos.Crear(afiliado, fecha: Datos.Hoy.AddDays(-i), servicios: servicio));
            ids.Add(result.Value.Autorizacion.Id);
        }

        return (afiliado, ids);
    }

    [Fact]
    public async Task Listar_RecorreTodasLasPaginasSinDuplicadosYDelMasRecienteAlMasAntiguo()
    {
        var (afiliado, ids) = await SembrarAsync(7);
        var vistos = new List<long>();
        string? cursor = null;
        var paginas = 0;

        do
        {
            var pagina = await fixture.SendAsync(new ListarAutorizacionesQuery(AfiliadoId: afiliado, Cursor: cursor, Limite: 3));
            pagina.Items.Count.ShouldBeLessThanOrEqualTo(3);
            vistos.AddRange(pagina.Items.Select(i => i.Id));
            cursor = pagina.NextCursor;
            paginas++;
        }
        while (cursor is not null);

        paginas.ShouldBe(3);
        vistos.ShouldBe(ids.OrderDescending().ToList());
    }

    [Fact]
    public async Task Listar_DevuelveResumenConNombresYCantidadDeServicios()
    {
        var (afiliado, _) = await SembrarAsync(1);

        var item = (await fixture.SendAsync(new ListarAutorizacionesQuery(AfiliadoId: afiliado))).Items.ShouldHaveSingleItem();

        item.Afiliado.ShouldStartWith("Prueba Integración");
        item.Prestador.ShouldBe("Clínica Central");
        item.ContratanteId.ShouldBe(3);
        item.CantidadServicios.ShouldBe(1);
        item.MontoArs.ShouldBe(70m);
    }

    [Fact]
    public async Task Listar_FiltraPorEstadoTipoDeServicioYFechas()
    {
        var (afiliado, ids) = await SembrarAsync(4);
        await fixture.SendAsync(new InactivarAutorizacionCommand(ids[0], "Prueba"));

        var activas = await fixture.SendAsync(new ListarAutorizacionesQuery(AfiliadoId: afiliado, Estado: EstadoAutorizacion.Activa));
        var inactivas = await fixture.SendAsync(new ListarAutorizacionesQuery(AfiliadoId: afiliado, Estado: EstadoAutorizacion.Inactiva));
        var laboratorio = await fixture.SendAsync(new ListarAutorizacionesQuery(AfiliadoId: afiliado, TipoServicioId: Datos.TipoLaboratorio));
        var ultimosDosDias = await fixture.SendAsync(new ListarAutorizacionesQuery(AfiliadoId: afiliado, FechaDesde: Datos.Hoy.AddDays(-1), FechaHasta: Datos.Hoy));

        activas.Items.Count.ShouldBe(3);
        inactivas.Items.ShouldHaveSingleItem().Id.ShouldBe(ids[0]);
        laboratorio.Items.Select(i => i.Id).ShouldBe([ids[3], ids[1]]);
        ultimosDosDias.Items.Select(i => i.Id).ShouldBe([ids[1], ids[0]]);
    }

    [Fact]
    public async Task Listar_FiltraPorNumeroYDocumentoDelAfiliado()
    {
        var (afiliado, ids) = await SembrarAsync(2);
        var numero = (await fixture.SendAsync(new ListarAutorizacionesQuery(AfiliadoId: afiliado))).Items.Single(i => i.Id == ids[1]).Numero;
        var documento = await fixture.QueryAsync(db => Task.FromResult(db.Afiliados.Single(a => a.Id == afiliado).Documento));

        var porNumero = await fixture.SendAsync(new ListarAutorizacionesQuery(Numero: numero.ToLowerInvariant()));
        var porDocumento = await fixture.SendAsync(new ListarAutorizacionesQuery(DocumentoAfiliado: documento));

        porNumero.Items.ShouldHaveSingleItem().Id.ShouldBe(ids[1]);
        porDocumento.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Listar_SinResultados_DevuelvePaginaVaciaSinCursor()
    {
        var afiliado = await fixture.CrearAfiliadoAsync();

        var pagina = await fixture.SendAsync(new ListarAutorizacionesQuery(AfiliadoId: afiliado));

        pagina.Items.ShouldBeEmpty();
        pagina.HasMore.ShouldBeFalse();
    }
}
