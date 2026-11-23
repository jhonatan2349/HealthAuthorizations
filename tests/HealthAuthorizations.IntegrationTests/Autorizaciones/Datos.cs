using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Autorizaciones.Crear;

namespace HealthAuthorizations.IntegrationTests.Autorizaciones;

/// <summary>Datos de prueba basados en el seed de catálogos.</summary>
internal static class Datos
{
    // Seed: afiliado 1 -> contratante 1, intermediario 1. Prestador 5 = Clínica Central.
    public const int AfiliadoSeed = 1;
    public const int PrestadorClinica = 5;
    public const int TipoConsulta = 1;
    public const int TipoLaboratorio = 4;
    public const int TipoImagenes = 5;

    public static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow);

    public static CrearAutorizacionCommand Crear(int afiliadoId = AfiliadoSeed, string? clave = null, DateOnly? fecha = null, params ServicioRequest[] servicios) =>
        new(
            afiliadoId,
            PrestadorClinica,
            fecha ?? Hoy,
            "Dolor abdominal",
            null,
            servicios.Length > 0 ? servicios : [new ServicioRequest(TipoConsulta, "Consulta general", 1500m), new ServicioRequest(TipoLaboratorio, "Hemograma", 333.33m)],
            clave);
}
