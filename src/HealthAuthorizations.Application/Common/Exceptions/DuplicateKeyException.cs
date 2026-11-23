namespace HealthAuthorizations.Application.Common.Exceptions;

/// <summary>
/// Violación de un índice único. Aísla a Application del proveedor de base de datos
/// (los códigos 2601/2627 de SQL Server se traducen en la infraestructura).
/// </summary>
public sealed class DuplicateKeyException : Exception
{
    public DuplicateKeyException()
    {
    }

    public DuplicateKeyException(string message)
        : base(message)
    {
    }

    public DuplicateKeyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
