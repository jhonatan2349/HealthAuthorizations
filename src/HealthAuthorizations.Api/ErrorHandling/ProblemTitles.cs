namespace HealthAuthorizations.Api.ErrorHandling;

/// <summary>Títulos y códigos estables de los ProblemDetails que emite la API.</summary>
internal static class ProblemTitles
{
    public const string Validacion = "Solicitud inválida";
    public const string NoEncontrado = "Recurso no encontrado";
    public const string Conflicto = "Conflicto con el estado actual del recurso";
    public const string VersionDesactualizada = "La versión del recurso no coincide";
    public const string ReglaDeNegocio = "Regla de negocio incumplida";
    public const string Interno = "Error interno";

    public const string CodigoValidacion = "Solicitud.Invalida";
    public const string CodigoConcurrencia = "Concurrencia.Conflicto";
    public const string CodigoDuplicado = "Recurso.Duplicado";
    public const string CodigoDominio = "Dominio.ReglaIncumplida";
    public const string CodigoInterno = "Error.Interno";

    public const string ExtensionCodigo = "code";
    public const string ExtensionTraceId = "traceId";
}
