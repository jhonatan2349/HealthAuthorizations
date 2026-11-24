using HealthAuthorizations.Application.Catalogos;
using HealthAuthorizations.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.IntegrationTests.Messaging;

public sealed class CacheCatalogosTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task CatalogoSimple_SegundaConsultaSeSirveDesdeCache()
    {
        var consulta = new ListarCatalogoQuery(TipoCatalogo.Intermediarios, IncluirInactivos: true);
        var primera = await fixture.SendAsync(consulta);
        var original = primera.Single(i => i.Id == 3).Nombre;

        try
        {
            await fixture.QueryAsync(db => db.Intermediarios.Where(i => i.Id == 3)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Nombre, "Cambiado en la base de datos"), TestContext.Current.CancellationToken));

            var segunda = await fixture.SendAsync(consulta);

            segunda.Single(i => i.Id == 3).Nombre.ShouldBe(original);
        }
        finally
        {
            await fixture.QueryAsync(db => db.Intermediarios.Where(i => i.Id == 3)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Nombre, original), TestContext.Current.CancellationToken));
        }
    }
}
