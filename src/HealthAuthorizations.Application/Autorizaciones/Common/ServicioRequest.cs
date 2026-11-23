namespace HealthAuthorizations.Application.Autorizaciones.Common;

/// <summary>Servicio a autorizar, tal como lo envía el cliente.</summary>
public sealed record ServicioRequest(int TipoServicioId, string Descripcion, decimal MontoReclamado);
