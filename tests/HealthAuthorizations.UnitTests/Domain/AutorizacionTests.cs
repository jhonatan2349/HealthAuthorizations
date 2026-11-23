using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.Domain.Autorizaciones.Events;
using HealthAuthorizations.Domain.Cobertura;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.UnitTests.Domain;

public sealed class AutorizacionTests
{
    private static readonly IPoliticaCobertura Politica70 = new PoliticaCoberturaPorcentual();
    private static readonly Participantes Participantes = new(afiliadoId: 1, prestadorId: 2, contratanteId: 3, intermediarioId: 4);
    private static readonly DateOnly Fecha = new(2026, 11, 20);

    private static Autorizacion CrearValida(params ServicioSolicitado[] servicios) =>
        Autorizacion.Crear(
            Participantes,
            Fecha,
            diagnostico: "Faringitis",
            observaciones: null,
            servicios.Length > 0 ? servicios : [new ServicioSolicitado(1, "Consulta general", 1000m)],
            Politica70,
            claveIdempotencia: "clave-1");

    [Fact]
    public void Crear_ConServiciosValidos_QuedaActivaConTotalesCalculados()
    {
        var autorizacion = CrearValida(
            new ServicioSolicitado(1, "Consulta", 1000m),
            new ServicioSolicitado(4, "Hemograma", 333.33m));

        autorizacion.Estado.ShouldBe(EstadoAutorizacion.Activa);
        autorizacion.Detalles.Count.ShouldBe(2);
        autorizacion.MontoReclamado.ShouldBe(1333.33m);
        autorizacion.MontoArs.ShouldBe(933.33m);
        autorizacion.MontoAfiliado.ShouldBe(400.00m);
        (autorizacion.MontoArs + autorizacion.MontoAfiliado).ShouldBe(autorizacion.MontoReclamado);
    }

    [Fact]
    public void Crear_AsignaParticipantesYGuardaElPorcentajeAplicadoEnCadaDetalle()
    {
        var autorizacion = CrearValida();

        autorizacion.AfiliadoId.ShouldBe(1);
        autorizacion.PrestadorId.ShouldBe(2);
        autorizacion.ContratanteId.ShouldBe(3);
        autorizacion.IntermediarioId.ShouldBe(4);
        autorizacion.ClaveIdempotencia.ShouldBe("clave-1");
        autorizacion.Detalles.ShouldAllBe(d => d.PorcentajeCobertura == 70m);
    }

    [Fact]
    public void Crear_EmiteEventoAutorizacionCreada()
    {
        var autorizacion = CrearValida();

        autorizacion.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<AutorizacionCreadaEvent>();
    }

    [Fact]
    public void Crear_SinServicios_Lanza()
    {
        var accion = () => Autorizacion.Crear(Participantes, Fecha, null, null, [], Politica70);

        Should.Throw<DomainException>(accion).Message.ShouldContain("al menos un servicio");
    }

    [Fact]
    public void Crear_ConMasServiciosDelMaximo_Lanza()
    {
        var servicios = Enumerable.Range(0, Autorizacion.MaxServicios + 1)
            .Select(i => new ServicioSolicitado(1, $"Servicio {i}", 10m))
            .ToList();

        Should.Throw<DomainException>(() => Autorizacion.Crear(Participantes, Fecha, null, null, servicios, Politica70));
    }

    [Fact]
    public void Crear_SinFechaDeServicio_Lanza() =>
        Should.Throw<DomainException>(() => Autorizacion.Crear(
            Participantes, default, null, null, [new ServicioSolicitado(1, "Consulta", 10m)], Politica70));

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Crear_ConMontoNoPositivo_Lanza(decimal monto) =>
        Should.Throw<DomainException>(() => CrearValida(new ServicioSolicitado(1, "Consulta", monto)));

    [Fact]
    public void Crear_ConServicioSinDescripcion_Lanza() =>
        Should.Throw<DomainException>(() => CrearValida(new ServicioSolicitado(1, "  ", 10m)));

    [Fact]
    public void Actualizar_ReemplazaServiciosYRecalculaConLaPoliticaVigente()
    {
        var autorizacion = CrearValida();
        autorizacion.ClearDomainEvents();

        autorizacion.Actualizar(
            prestadorId: 9,
            Fecha.AddDays(1),
            diagnostico: null,
            observaciones: "Ajuste",
            [new ServicioSolicitado(5, "Radiografía", 2000m)],
            new PoliticaCoberturaPorcentual(80m));

        autorizacion.PrestadorId.ShouldBe(9);
        autorizacion.Detalles.ShouldHaveSingleItem().TipoServicioId.ShouldBe(5);
        autorizacion.MontoArs.ShouldBe(1600m);
        autorizacion.MontoAfiliado.ShouldBe(400m);
        autorizacion.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<AutorizacionActualizadaEvent>();
    }

    [Fact]
    public void Actualizar_ConServicioInvalido_NoModificaElAgregado()
    {
        var autorizacion = CrearValida();

        Should.Throw<DomainException>(() => autorizacion.Actualizar(
            9,
            Fecha,
            null,
            null,
            [new ServicioSolicitado(1, "Válido", 10m), new ServicioSolicitado(1, "Inválido", -1m)],
            Politica70));

        autorizacion.PrestadorId.ShouldBe(2);
        autorizacion.MontoReclamado.ShouldBe(1000m);
        autorizacion.Detalles.Count.ShouldBe(1);
    }

    [Fact]
    public void Inactivar_CambiaEstadoConservaDatosYEmiteEvento()
    {
        var autorizacion = CrearValida();
        autorizacion.ClearDomainEvents();
        var fecha = new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

        autorizacion.Inactivar("Solicitud del prestador", fecha);

        autorizacion.Estado.ShouldBe(EstadoAutorizacion.Inactiva);
        autorizacion.FechaInactivacion.ShouldBe(fecha);
        autorizacion.MotivoInactivacion.ShouldBe("Solicitud del prestador");
        autorizacion.Detalles.Count.ShouldBe(1);

        var evento = autorizacion.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<AutorizacionEstadoCambiadoEvent>();
        evento.EstadoAnterior.ShouldBe(EstadoAutorizacion.Activa);
        evento.EstadoNuevo.ShouldBe(EstadoAutorizacion.Inactiva);
    }

    [Fact]
    public void Inactivar_SinMotivo_Lanza() =>
        Should.Throw<DomainException>(() => CrearValida().Inactivar(" ", DateTimeOffset.UtcNow));

    [Fact]
    public void Inactivar_DosVeces_Lanza()
    {
        var autorizacion = CrearValida();
        autorizacion.Inactivar("Duplicada", DateTimeOffset.UtcNow);

        Should.Throw<DomainException>(() => autorizacion.Inactivar("Otra vez", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Actualizar_AutorizacionInactiva_Lanza()
    {
        var autorizacion = CrearValida();
        autorizacion.Inactivar("Error de digitación", DateTimeOffset.UtcNow);

        Should.Throw<DomainException>(() => autorizacion.Actualizar(
            2, Fecha, null, null, [new ServicioSolicitado(1, "Consulta", 10m)], Politica70));
    }

    [Theory]
    [InlineData(0, 2, 3)]
    [InlineData(1, 0, 3)]
    [InlineData(1, 2, -1)]
    public void Participantes_ConIdentificadorInvalido_Lanza(int afiliado, int prestador, int contratante) =>
        Should.Throw<DomainException>(() => new Participantes(afiliado, prestador, contratante, null));
}
