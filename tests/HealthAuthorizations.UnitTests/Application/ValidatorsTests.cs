using FluentValidation.TestHelper;
using HealthAuthorizations.Application.Autorizaciones.Actualizar;
using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Autorizaciones.Crear;
using HealthAuthorizations.Application.Autorizaciones.Inactivar;
using HealthAuthorizations.Application.Autorizaciones.Listar;
using HealthAuthorizations.Application.Common.Pagination;
using HealthAuthorizations.Domain.Autorizaciones;
using HealthAuthorizations.UnitTests.TestDoubles;

namespace HealthAuthorizations.UnitTests.Application;

public sealed class ValidatorsTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(FixedTimeProvider.Default.UtcDateTime);
    private static readonly ServicioRequest ServicioValido = new(1, "Consulta", 1000m);

    private readonly CrearAutorizacionValidator _crear = new(new FixedTimeProvider());
    private readonly ActualizarAutorizacionValidator _actualizar = new(new FixedTimeProvider());
    private readonly InactivarAutorizacionValidator _inactivar = new();
    private readonly ListarAutorizacionesValidator _listar = new();

    private static CrearAutorizacionCommand Crear(params ServicioRequest[] servicios) =>
        new(1, 2, Hoy, null, null, servicios.Length > 0 ? servicios : [ServicioValido]);

    [Fact]
    public void Crear_Valido_NoTieneErrores() =>
        _crear.TestValidate(Crear()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Crear_SinServicios_TieneError() =>
        _crear.TestValidate(Crear() with { Servicios = [] }).ShouldHaveValidationErrorFor(x => x.Servicios);

    [Fact]
    public void Crear_ConDemasiadosServicios_TieneError()
    {
        var servicios = Enumerable.Repeat(ServicioValido, Autorizacion.MaxServicios + 1).ToList();

        _crear.TestValidate(Crear() with { Servicios = servicios }).ShouldHaveValidationErrorFor(x => x.Servicios);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Crear_ConIdsInvalidos_TieneErrores(int id)
    {
        var resultado = _crear.TestValidate(Crear() with { AfiliadoId = id, PrestadorId = id });

        resultado.ShouldHaveValidationErrorFor(x => x.AfiliadoId);
        resultado.ShouldHaveValidationErrorFor(x => x.PrestadorId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.001)]
    public void Crear_ConMontoInvalido_TieneError(decimal monto) =>
        _crear.TestValidate(Crear(new ServicioRequest(1, "Consulta", monto)))
            .ShouldHaveValidationErrorFor("Servicios[0].MontoReclamado");

    [Fact]
    public void Crear_ConServicioSinDescripcionNiTipo_TieneErrores()
    {
        var resultado = _crear.TestValidate(Crear(new ServicioRequest(0, "", 10m)));

        resultado.ShouldHaveValidationErrorFor("Servicios[0].TipoServicioId");
        resultado.ShouldHaveValidationErrorFor("Servicios[0].Descripcion");
    }

    [Fact]
    public void Crear_ConFechaMuyEnElFuturo_TieneError() =>
        _crear.TestValidate(Crear() with { FechaServicio = Hoy.AddDays(DatosAutorizacionValidator.MaxDiasFuturo + 1) })
            .ShouldHaveValidationErrorFor(x => x.FechaServicio);

    [Fact]
    public void Crear_ConFechaEnElLimitePermitido_EsValida() =>
        _crear.TestValidate(Crear() with { FechaServicio = Hoy.AddDays(DatosAutorizacionValidator.MaxDiasFuturo) })
            .ShouldNotHaveValidationErrorFor(x => x.FechaServicio);

    [Fact]
    public void Crear_ConTextosDemasiadoLargos_TieneErrores()
    {
        var resultado = _crear.TestValidate(Crear() with
        {
            Diagnostico = new string('x', Autorizacion.MaxLongitudDiagnostico + 1),
            Observaciones = new string('x', Autorizacion.MaxLongitudObservaciones + 1),
            ClaveIdempotencia = new string('x', Autorizacion.MaxLongitudClaveIdempotencia + 1),
        });

        resultado.ShouldHaveValidationErrorFor(x => x.Diagnostico);
        resultado.ShouldHaveValidationErrorFor(x => x.Observaciones);
        resultado.ShouldHaveValidationErrorFor(x => x.ClaveIdempotencia);
    }

    [Fact]
    public void Actualizar_ConVersionInvalida_TieneError() =>
        _actualizar.TestValidate(new ActualizarAutorizacionCommand(1, 2, Hoy, null, null, [ServicioValido], "no-valida"))
            .ShouldHaveValidationErrorFor(x => x.Version);

    [Fact]
    public void Actualizar_ConVersionValida_NoTieneErrores() =>
        _actualizar.TestValidate(new ActualizarAutorizacionCommand(1, 2, Hoy, null, null, [ServicioValido], VersionToken.Encode(new byte[8])))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Inactivar_SinMotivo_TieneError() =>
        _inactivar.TestValidate(new InactivarAutorizacionCommand(1, " ")).ShouldHaveValidationErrorFor(x => x.Motivo);

    [Fact]
    public void Inactivar_Valido_NoTieneErrores() =>
        _inactivar.TestValidate(new InactivarAutorizacionCommand(1, "Duplicada")).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(0)]
    [InlineData(ListarAutorizacionesQuery.LimiteMaximo + 1)]
    public void Listar_ConLimiteFueraDeRango_TieneError(int limite) =>
        _listar.TestValidate(new ListarAutorizacionesQuery(Limite: limite)).ShouldHaveValidationErrorFor(x => x.Limite);

    [Fact]
    public void Listar_ConCursorInvalido_TieneError() =>
        _listar.TestValidate(new ListarAutorizacionesQuery(Cursor: "basura")).ShouldHaveValidationErrorFor(x => x.Cursor);

    [Fact]
    public void Listar_ConRangoDeFechasInvertido_TieneError() =>
        _listar.TestValidate(new ListarAutorizacionesQuery(FechaDesde: Hoy, FechaHasta: Hoy.AddDays(-1)))
            .ShouldHaveValidationErrorFor("FechaDesde");

    [Fact]
    public void Listar_ConEstadoInexistente_TieneError() =>
        _listar.TestValidate(new ListarAutorizacionesQuery(Estado: (EstadoAutorizacion)99)).ShouldHaveValidationErrorFor(x => x.Estado);

    [Fact]
    public void Listar_ConFiltrosValidos_NoTieneErrores() =>
        _listar.TestValidate(new ListarAutorizacionesQuery(
                AfiliadoId: 1, Estado: EstadoAutorizacion.Activa, FechaDesde: Hoy.AddDays(-30), FechaHasta: Hoy, Cursor: Cursor.Encode(500), Limite: 50))
            .ShouldNotHaveAnyValidationErrors();
}
