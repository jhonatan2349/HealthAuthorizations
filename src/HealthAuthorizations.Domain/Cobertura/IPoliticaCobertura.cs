namespace HealthAuthorizations.Domain.Cobertura;

/// <summary>
/// Regla de distribución de montos (patrón Strategy).
/// Permite cambiar la regla (porcentaje distinto, por tipo de servicio, por plan, con topes)
/// sin modificar el agregado <c>Autorizacion</c> ni los casos de uso.
/// </summary>
public interface IPoliticaCobertura
{
    DistribucionMonto Distribuir(int tipoServicioId, decimal montoReclamado);
}
