using HealthAuthorizations.Domain.Autorizaciones.Events;
using HealthAuthorizations.Domain.Cobertura;
using HealthAuthorizations.Domain.Common;

namespace HealthAuthorizations.Domain.Autorizaciones;

/// <summary>
/// Raíz de agregado. Garantiza que toda autorización tenga al menos un servicio,
/// que los totales coincidan con sus detalles y que una autorización inactiva no se modifique.
/// </summary>
public sealed class Autorizacion : AggregateRoot<long>
{
    public const int MaxLongitudNumero = 14;
    public const int MaxLongitudDiagnostico = 250;
    public const int MaxLongitudObservaciones = 1000;
    public const int MaxLongitudMotivoInactivacion = 500;
    public const int MaxLongitudClaveIdempotencia = 64;
    public const int MaxServicios = 50;

    private readonly List<AutorizacionDetalle> _detalles = [];

    private Autorizacion()
    {
    }

    /// <summary>Código legible (AUT-0000000001). Lo genera la base de datos a partir del Id.</summary>
    public string Numero { get; private set; } = string.Empty;

    public int AfiliadoId { get; private set; }

    public int PrestadorId { get; private set; }

    public int ContratanteId { get; private set; }

    public int? IntermediarioId { get; private set; }

    public DateOnly FechaServicio { get; private set; }

    public string? Diagnostico { get; private set; }

    public string? Observaciones { get; private set; }

    public EstadoAutorizacion Estado { get; private set; }

    public DateTimeOffset? FechaInactivacion { get; private set; }

    public string? MotivoInactivacion { get; private set; }

    /// <summary>Totales desnormalizados para listar sin leer los detalles.</summary>
    public decimal MontoReclamado { get; private set; }

    public decimal MontoArs { get; private set; }

    public decimal MontoAfiliado { get; private set; }

    /// <summary>Clave enviada por el cliente (header Idempotency-Key); única para evitar duplicados por reintentos.</summary>
    public string? ClaveIdempotencia { get; private set; }

    /// <summary>Token de concurrencia optimista (rowversion). Se expone como ETag en la API.</summary>
#pragma warning disable CA1819 // rowversion de SQL Server se mapea a byte[].
    public byte[] RowVersion { get; private set; } = [];
#pragma warning restore CA1819

    public IReadOnlyCollection<AutorizacionDetalle> Detalles => _detalles.AsReadOnly();

    public bool EstaActiva => Estado == EstadoAutorizacion.Activa;

    public static Autorizacion Crear(
        Participantes participantes,
        DateOnly fechaServicio,
        string? diagnostico,
        string? observaciones,
        IReadOnlyCollection<ServicioSolicitado> servicios,
        IPoliticaCobertura politica,
        string? claveIdempotencia = null)
    {
        ArgumentNullException.ThrowIfNull(participantes);

        var autorizacion = new Autorizacion
        {
            AfiliadoId = participantes.AfiliadoId,
            ContratanteId = participantes.ContratanteId,
            IntermediarioId = participantes.IntermediarioId,
            Estado = EstadoAutorizacion.Activa,
            ClaveIdempotencia = Guard.Opcional(claveIdempotencia, nameof(ClaveIdempotencia), MaxLongitudClaveIdempotencia),
        };

        autorizacion.AplicarDatos(participantes.PrestadorId, fechaServicio, diagnostico, observaciones, servicios, politica);
        autorizacion.RaiseDomainEvent(new AutorizacionCreadaEvent(autorizacion));

        return autorizacion;
    }

    /// <summary>
    /// Reemplaza los datos modificables. El afiliado no cambia: otra persona implica otra autorización.
    /// Los montos se recalculan con la política vigente.
    /// </summary>
    public void Actualizar(
        int prestadorId,
        DateOnly fechaServicio,
        string? diagnostico,
        string? observaciones,
        IReadOnlyCollection<ServicioSolicitado> servicios,
        IPoliticaCobertura politica)
    {
        AsegurarActiva();
        AplicarDatos(prestadorId, fechaServicio, diagnostico, observaciones, servicios, politica);
        RaiseDomainEvent(new AutorizacionActualizadaEvent(this));
    }

    /// <summary>Inactivación lógica: el registro se conserva para fines históricos.</summary>
    public void Inactivar(string motivo, DateTimeOffset fecha)
    {
        AsegurarActiva();

        MotivoInactivacion = Guard.Requerido(motivo, nameof(MotivoInactivacion), MaxLongitudMotivoInactivacion);
        FechaInactivacion = fecha;
        Estado = EstadoAutorizacion.Inactiva;

        RaiseDomainEvent(new AutorizacionEstadoCambiadoEvent(this, EstadoAutorizacion.Activa, EstadoAutorizacion.Inactiva));
    }

    private void AplicarDatos(
        int prestadorId,
        DateOnly fechaServicio,
        string? diagnostico,
        string? observaciones,
        IReadOnlyCollection<ServicioSolicitado> servicios,
        IPoliticaCobertura politica)
    {
        ArgumentNullException.ThrowIfNull(politica);

        if (servicios is null || servicios.Count == 0)
        {
            throw new DomainException("La autorización debe contener al menos un servicio.");
        }

        if (servicios.Count > MaxServicios)
        {
            throw new DomainException($"La autorización no puede contener más de {MaxServicios} servicios.");
        }

        if (fechaServicio == default)
        {
            throw new DomainException("La fecha del servicio es obligatoria.");
        }

        // Se construyen todos los detalles antes de mutar el estado: si uno falla, el agregado queda intacto.
        var detalles = servicios.Select(s => AutorizacionDetalle.Crear(s, politica)).ToList();

        PrestadorId = Guard.IdValido(prestadorId, nameof(PrestadorId));
        FechaServicio = fechaServicio;
        Diagnostico = Guard.Opcional(diagnostico, nameof(Diagnostico), MaxLongitudDiagnostico);
        Observaciones = Guard.Opcional(observaciones, nameof(Observaciones), MaxLongitudObservaciones);

        _detalles.Clear();
        _detalles.AddRange(detalles);
        RecalcularTotales();
    }

    private void RecalcularTotales()
    {
        MontoReclamado = _detalles.Sum(d => d.MontoReclamado);
        MontoArs = _detalles.Sum(d => d.MontoArs);
        MontoAfiliado = _detalles.Sum(d => d.MontoAfiliado);
    }

    private void AsegurarActiva()
    {
        if (!EstaActiva)
        {
            throw new DomainException("La autorización está inactiva y no puede modificarse.");
        }
    }
}
