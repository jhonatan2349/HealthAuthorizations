namespace HealthAuthorizations.Application.Common.Results;

/// <summary>
/// Resultado de un caso de uso. Los errores esperados (no encontrado, conflicto, regla de negocio)
/// se devuelven como valor en lugar de lanzar excepciones, lo que hace explícito el flujo y evita su costo.
/// </summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result(Error error) => Failure(error);
}

#pragma warning disable CA1000 // Las fábricas estáticas en el tipo genérico son la API idiomática de Result<T>.
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(null) => _value = value;

    private Result(Error error)
        : base(error)
    {
    }

    /// <summary>Valor del resultado exitoso. Acceder a él en un fallo es un error de programación.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede acceder al valor de un resultado fallido.");

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
#pragma warning restore CA1000
