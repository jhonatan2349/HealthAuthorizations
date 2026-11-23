using HealthAuthorizations.Application.Common.Results;
using MediatR;

namespace HealthAuthorizations.Application.Autorizaciones.Inactivar;

/// <summary>Inactivación lógica; la autorización se conserva para fines históricos.</summary>
/// <param name="Version">Valor de If-Match; si se envía y no coincide, la operación falla por concurrencia.</param>
public sealed record InactivarAutorizacionCommand(long Id, string Motivo, string? Version = null) : IRequest<Result>;
