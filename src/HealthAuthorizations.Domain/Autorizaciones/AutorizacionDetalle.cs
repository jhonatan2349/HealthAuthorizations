using HealthAuthorizations.Domain.Catalogos;
using HealthAuthorizations.Domain.Cobertura;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Autorizaciones;

/// <summary>
/// Servicio autorizado. Solo se crea a través del agregado <see cref="Autorizacion"/>.
/// Guarda el porcentaje aplicado para que el histórico no cambie si cambia la política.
/// </summary>
public sealed class AutorizacionDetalle : Entity<long>
{
    public const int MaxLongitudDescripcion = 250;

    private AutorizacionDetalle()
    {
    }

    private AutorizacionDetalle(int tipoServicioId, string descripcion, DistribucionMonto distribucion)
    {
        TipoServicioId = Guard.IdValido(tipoServicioId, nameof(TipoServicioId));
        Descripcion = Guard.Requerido(descripcion, nameof(Descripcion), MaxLongitudDescripcion);
        MontoReclamado = distribucion.MontoReclamado;
        PorcentajeCobertura = distribucion.PorcentajeArs;
        MontoArs = distribucion.MontoArs;
        MontoAfiliado = distribucion.MontoAfiliado;
    }

    public long AutorizacionId { get; private set; }

    public int TipoServicioId { get; private set; }

    public TipoServicio? TipoServicio { get; private set; }

    public string Descripcion { get; private set; } = string.Empty;

    public decimal MontoReclamado { get; private set; }

    public decimal PorcentajeCobertura { get; private set; }

    public decimal MontoArs { get; private set; }

    public decimal MontoAfiliado { get; private set; }

    internal static AutorizacionDetalle Crear(ServicioSolicitado servicio, IPoliticaCobertura politica)
    {
        var distribucion = politica.Distribuir(servicio.TipoServicioId, servicio.MontoReclamado);
        return new AutorizacionDetalle(servicio.TipoServicioId, servicio.Descripcion, distribucion);
    }
}
