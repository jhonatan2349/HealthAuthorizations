using HealthAuthorizations.Application.Common.Results;

namespace HealthAuthorizations.UnitTests.Application;

public sealed class ResultTests
{
    private static readonly Error ErrorDePrueba = Error.NotFound("Prueba.NoEncontrada", "No existe.");

    [Fact]
    public void Success_EsExitosoSinError()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void ConversionImplicitaDesdeError_EsFallo()
    {
        Result result = ErrorDePrueba;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ErrorDePrueba);
    }

    [Fact]
    public void ResultGenerico_ConValor_ExponeElValor()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void ResultGenerico_Fallido_NoPermiteLeerElValor()
    {
        Result<int> result = ErrorDePrueba;

        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_ConErrorNulo_Lanza()
    {
        Should.Throw<ArgumentNullException>(() => Result.Failure(null!));
        Should.Throw<ArgumentNullException>(() => Result<int>.Failure(null!));
    }

    [Theory]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.BusinessRule)]
    [InlineData(ErrorType.Validation)]
    public void FabricasDeError_AsignanElTipo(ErrorType tipo)
    {
        var error = tipo switch
        {
            ErrorType.NotFound => Error.NotFound("c", "m"),
            ErrorType.Conflict => Error.Conflict("c", "m"),
            ErrorType.BusinessRule => Error.BusinessRule("c", "m"),
            _ => Error.Validation("c", "m"),
        };

        error.Type.ShouldBe(tipo);
    }
}
