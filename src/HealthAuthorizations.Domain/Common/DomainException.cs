namespace HealthAuthorizations.Domain.Common;

/// <summary>
/// Violación de una regla de negocio o invariante del dominio.
/// La API la traduce a 422 Unprocessable Entity.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
