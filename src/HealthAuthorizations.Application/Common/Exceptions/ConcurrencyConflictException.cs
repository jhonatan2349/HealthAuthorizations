namespace HealthAuthorizations.Application.Common.Exceptions;

/// <summary>
/// Otro usuario modificó el recurso desde que se leyó (rowversion distinto).
/// La lanza la infraestructura; la API la traduce a 412 (con If-Match) o 409.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("El recurso fue modificado por otra operación. Vuelva a consultarlo e intente de nuevo.")
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
