namespace HealthAuthorizations.Api.Security;

/// <summary>Roles reconocidos en el claim "role" del token.</summary>
public static class Roles
{
    /// <summary>Consulta autorizaciones y catálogos.</summary>
    public const string Lector = "autorizaciones.lector";

    /// <summary>Además de consultar, crea, modifica e inactiva autorizaciones.</summary>
    public const string Operador = "autorizaciones.operador";
}

/// <summary>Políticas de autorización aplicadas en los controllers.</summary>
public static class Politicas
{
    public const string Lectura = "autorizaciones:lectura";
    public const string Escritura = "autorizaciones:escritura";
}

/// <summary>Nombres de claims (sin el mapeo heredado de WS-Federation).</summary>
public static class Claims
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Role = "role";
}
