using System.Reflection;
using NetArchTest.Rules;

namespace HealthAuthorizations.ArchitectureTests;

/// <summary>
/// Verifica la regla de dependencias de Clean Architecture:
/// Api -> Application -> Domain; Infrastructure -> Application.
/// </summary>
public sealed class LayerDependencyTests
{
    private const string DomainNamespace = "HealthAuthorizations.Domain";
    private const string ApplicationNamespace = "HealthAuthorizations.Application";
    private const string InfrastructureNamespace = "HealthAuthorizations.Infrastructure";
    private const string ApiNamespace = "HealthAuthorizations.Api";

    private static readonly Assembly DomainAssembly = Assembly.Load(DomainNamespace);
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = Assembly.Load(InfrastructureNamespace);

    [Fact]
    public void Domain_NoDebeDepender_DeNingunaOtraCapa()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Domain_NoDebeDepender_DeFrameworksDeInfraestructura()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "MediatR", "Mapster")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Application_NoDebeDepender_DeInfrastructureNiApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Infrastructure_NoDebeDepender_DeApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    private static string Describe(NetArchTest.Rules.TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : $"Tipos que violan la regla: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
