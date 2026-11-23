namespace HealthAuthorizations.Application.Common.Results;

/// <summary>Categoría del error; la API la traduce a un código HTTP.</summary>
public enum ErrorType
{
    /// <summary>Entrada inválida (400).</summary>
    Validation,

    /// <summary>El recurso solicitado no existe (404).</summary>
    NotFound,

    /// <summary>El estado actual del recurso impide la operación (409).</summary>
    Conflict,

    /// <summary>La solicitud es válida pero viola una regla de negocio (422).</summary>
    BusinessRule,
}
