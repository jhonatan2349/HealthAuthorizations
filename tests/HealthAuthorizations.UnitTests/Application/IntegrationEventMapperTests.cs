using HealthAuthorizations.Application.IntegrationEvents;
using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.Domain.Cobertura;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.UnitTests.Application;

public sealed class IntegrationEventMapperTests
{
    private static Autorizacion Nueva() => Autorizacion.Crear(
        new Participantes(1, 2, 3, null),
        new DateOnly(2026, 11, 20),
        "Diagnóstico confidencial",
        "Observación",
        [new ServicioSolicitado(1, "Consulta", 1000m), new ServicioSolicitado(4, "Laboratorio", 500m)],
        new PoliticaCoberturaPorcentual());

    [Fact]
    public void Creada_SeMapeaConMontosYSinDatosClinicos()
    {
        var autorizacion = Nueva();

        var evento = IntegrationEventMapper.Map(autorizacion.DomainEvents.Single()).ShouldBeOfType<AutorizacionCreadaV1>();

        evento.Tipo.ShouldBe("autorizacion.creada.v1");
        evento.MontoArs.ShouldBe(1050m);
        evento.MontoAfiliado.ShouldBe(450m);
        evento.CantidadServicios.ShouldBe(2);
        evento.IntermediarioId.ShouldBeNull();
        typeof(AutorizacionCreadaV1).GetProperties().Select(p => p.Name).ShouldNotContain("Diagnostico");
    }

    [Fact]
    public void EstadoCambiado_SeMapeaConEstadosComoTextoYSinMotivo()
    {
        var autorizacion = Nueva();
        autorizacion.ClearDomainEvents();
        autorizacion.Inactivar("Motivo con datos del paciente", DateTimeOffset.UtcNow);

        var evento = IntegrationEventMapper.Map(autorizacion.DomainEvents.Single()).ShouldBeOfType<AutorizacionEstadoCambiadoV1>();

        evento.EstadoAnterior.ShouldBe("Activa");
        evento.EstadoNuevo.ShouldBe("Inactiva");
        typeof(AutorizacionEstadoCambiadoV1).GetProperties().Select(p => p.Name).ShouldNotContain("Motivo");
    }

    [Fact]
    public void Actualizada_SeMapea()
    {
        var autorizacion = Nueva();
        autorizacion.ClearDomainEvents();
        autorizacion.Actualizar(9, new DateOnly(2026, 11, 21), null, null, [new ServicioSolicitado(5, "Imagen", 200m)], new PoliticaCoberturaPorcentual());

        var evento = IntegrationEventMapper.Map(autorizacion.DomainEvents.Single()).ShouldBeOfType<AutorizacionActualizadaV1>();

        evento.PrestadorId.ShouldBe(9);
        evento.MontoReclamado.ShouldBe(200m);
    }

    private sealed record EventoDesconocido : IDomainEvent;

    [Fact]
    public void EventoSinContratoPublico_NoSePublica() =>
        IntegrationEventMapper.Map(new EventoDesconocido()).ShouldBeNull();
}
