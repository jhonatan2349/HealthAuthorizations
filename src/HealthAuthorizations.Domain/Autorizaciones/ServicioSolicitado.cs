namespace HealthAuthorizations.Domain.Autorizaciones;

/// <summary>Datos de entrada de un servicio a autorizar.</summary>
public sealed record ServicioSolicitado(int TipoServicioId, string Descripcion, decimal MontoReclamado);
