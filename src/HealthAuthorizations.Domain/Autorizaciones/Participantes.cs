using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Autorizaciones;

/// <summary>
/// Partes involucradas en la autorización. Contratante e intermediario se toman del afiliado
/// al momento de autorizar y se guardan como snapshot: si el afiliado cambia de empresa,
/// el histórico conserva quién respondía por él en ese momento.
/// </summary>
public sealed record Participantes
{
    public Participantes(int afiliadoId, int prestadorId, int contratanteId, int? intermediarioId)
    {
        AfiliadoId = Guard.IdValido(afiliadoId, nameof(AfiliadoId));
        PrestadorId = Guard.IdValido(prestadorId, nameof(PrestadorId));
        ContratanteId = Guard.IdValido(contratanteId, nameof(ContratanteId));
        IntermediarioId = intermediarioId is null ? null : Guard.IdValido(intermediarioId.Value, nameof(IntermediarioId));
    }

    public int AfiliadoId { get; }

    public int PrestadorId { get; }

    public int ContratanteId { get; }

    public int? IntermediarioId { get; }
}
