using FluentValidation;
using HealthAuthorizations.Application.Common.Behaviors;
using HealthAuthorizations.Application.Common.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace HealthAuthorizations.UnitTests.Application;

public sealed class BehaviorsTests
{
    public sealed record Solicitud(string Nombre);

    private sealed class SolicitudValidator : AbstractValidator<Solicitud>
    {
        public SolicitudValidator() => RuleFor(x => x.Nombre).NotEmpty();
    }

    [Fact]
    public async Task Validation_ConSolicitudValida_LlamaAlHandler()
    {
        var behavior = new ValidationBehavior<Solicitud, string>([new SolicitudValidator()]);
        var llamado = false;

        var respuesta = await behavior.Handle(new Solicitud("ok"), _ => { llamado = true; return Task.FromResult("hecho"); }, CancellationToken.None);

        llamado.ShouldBeTrue();
        respuesta.ShouldBe("hecho");
    }

    [Fact]
    public async Task Validation_ConSolicitudInvalida_LanzaSinLlamarAlHandler()
    {
        var behavior = new ValidationBehavior<Solicitud, string>([new SolicitudValidator()]);
        var llamado = false;

        var ex = await Should.ThrowAsync<ValidationException>(() =>
            behavior.Handle(new Solicitud(""), _ => { llamado = true; return Task.FromResult("x"); }, CancellationToken.None));

        llamado.ShouldBeFalse();
        ex.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(Solicitud.Nombre));
    }

    [Fact]
    public async Task Validation_SinValidadores_LlamaAlHandler()
    {
        var behavior = new ValidationBehavior<Solicitud, string>([]);

        var respuesta = await behavior.Handle(new Solicitud(""), _ => Task.FromResult("hecho"), CancellationToken.None);

        respuesta.ShouldBe("hecho");
    }

    [Fact]
    public async Task Logging_ConResultadoExitoso_RegistraInformacion()
    {
        var logger = CrearLogger<Result>();
        var behavior = new LoggingBehavior<Solicitud, Result>(logger);

        var respuesta = await behavior.Handle(new Solicitud("x"), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        respuesta.IsSuccess.ShouldBeTrue();
        NivelesRegistrados(logger).ShouldBe([LogLevel.Information]);
    }

    [Fact]
    public async Task Logging_ConResultadoFallido_RegistraAdvertencia()
    {
        var logger = CrearLogger<Result>();
        var behavior = new LoggingBehavior<Solicitud, Result>(logger);

        await behavior.Handle(new Solicitud("x"), _ => Task.FromResult(Result.Failure(Error.NotFound("X", "y"))), CancellationToken.None);

        NivelesRegistrados(logger).ShouldBe([LogLevel.Warning]);
    }

    [Fact]
    public async Task Logging_ConSolicitudLenta_RegistraAlertaDeLentitud()
    {
        var logger = CrearLogger<string>();
        var behavior = new LoggingBehavior<Solicitud, string>(logger);

        await behavior.Handle(
            new Solicitud("x"),
            async ct => { await Task.Delay(LoggingBehavior<Solicitud, string>.UmbralLentoMs + 50, ct); return "ok"; },
            CancellationToken.None);

        NivelesRegistrados(logger).ShouldBe([LogLevel.Information, LogLevel.Warning]);
    }

    private static ILogger<LoggingBehavior<Solicitud, TResponse>> CrearLogger<TResponse>()
    {
        var logger = Substitute.For<ILogger<LoggingBehavior<Solicitud, TResponse>>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        return logger;
    }

    private static LogLevel[] NivelesRegistrados(ILogger logger) =>
        logger.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(c => (LogLevel)c.GetArguments()[0]!)
            .ToArray();
}
