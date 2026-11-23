namespace HealthAuthorizations.Domain.Common;

/// <summary>
/// Hecho relevante del negocio. Se publica tras persistir el agregado (Outbox en la Fase 7).
/// No depende de MediatR para mantener el dominio libre de frameworks.
/// </summary>
#pragma warning disable CA1040 // Interfaz marcador intencional: identifica eventos de dominio.
public interface IDomainEvent;
#pragma warning restore CA1040
