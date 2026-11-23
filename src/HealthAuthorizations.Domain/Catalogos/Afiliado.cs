using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Catalogos;

/// <summary>Persona cubierta por el plan de salud, vinculada a un contratante y, opcionalmente, a un intermediario.</summary>
public sealed class Afiliado : AuditableEntity<int>
{
    public const int MaxLongitudNumeroAfiliado = 20;
    public const int MaxLongitudDocumento = 15;
    public const int MaxLongitudNombres = 100;
    public const int MaxLongitudApellidos = 100;

    private Afiliado()
    {
    }

    public Afiliado(
        string numeroAfiliado,
        string documento,
        string nombres,
        string apellidos,
        DateOnly fechaNacimiento,
        int contratanteId,
        int? intermediarioId)
    {
        NumeroAfiliado = Guard.Requerido(numeroAfiliado, nameof(NumeroAfiliado), MaxLongitudNumeroAfiliado);
        Documento = Guard.Requerido(documento, nameof(Documento), MaxLongitudDocumento);
        Nombres = Guard.Requerido(nombres, nameof(Nombres), MaxLongitudNombres);
        Apellidos = Guard.Requerido(apellidos, nameof(Apellidos), MaxLongitudApellidos);
        FechaNacimiento = fechaNacimiento;
        ContratanteId = Guard.IdValido(contratanteId, nameof(ContratanteId));
        IntermediarioId = intermediarioId is null ? null : Guard.IdValido(intermediarioId.Value, nameof(IntermediarioId));
        Activo = true;
    }

    public string NumeroAfiliado { get; private set; } = string.Empty;

    /// <summary>Cédula o pasaporte.</summary>
    public string Documento { get; private set; } = string.Empty;

    public string Nombres { get; private set; } = string.Empty;

    public string Apellidos { get; private set; } = string.Empty;

    public DateOnly FechaNacimiento { get; private set; }

    public int ContratanteId { get; private set; }

    public Contratante? Contratante { get; private set; }

    public int? IntermediarioId { get; private set; }

    public Intermediario? Intermediario { get; private set; }

    public bool Activo { get; private set; }

    public void Activar() => Activo = true;

    public void Desactivar() => Activo = false;
}
