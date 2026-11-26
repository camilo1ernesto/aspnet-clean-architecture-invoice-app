using System.Reflection;
using NetArchTest.Rules;

namespace Invoicing.ArchitectureTests;

/// <summary>
/// Enforces that the inner layers stay free of persistence and HTTP technologies
/// (ADR-001 Decisions 1 and 2, docs/architecture/architecture.md DOM-1 and APP-2).
/// </summary>
public class TechnologyDependencyTests
{
    private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    private const string AspNetCore = "Microsoft.AspNetCore";

    private static readonly Assembly DomainAssembly = typeof(Domain.Abstractions.BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;

    // ADR-001 Decision 1 ("must not depend on ... persistence technologies"), DOM-1.
    [Fact]
    public void Domain_should_not_depend_on_EntityFrameworkCore() =>
        AssertNoDependency(DomainAssembly, EntityFrameworkCore);

    // ADR-001 Decision 2 ("contains no infrastructure implementation details"), APP-2 ("no ORM types").
    [Fact]
    public void Application_should_not_depend_on_EntityFrameworkCore() =>
        AssertNoDependency(ApplicationAssembly, EntityFrameworkCore);

    // ADR-001 Decision 2, APP-2 ("no HTTP types").
    [Fact]
    public void Application_should_not_depend_on_AspNetCore() =>
        AssertNoDependency(ApplicationAssembly, AspNetCore);

    private static void AssertNoDependency(Assembly assembly, string forbiddenNamespace)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        var failingTypes = result.FailingTypeNames ?? [];
        Assert.True(result.IsSuccessful,
            $"{assembly.GetName().Name} must not depend on {forbiddenNamespace} (ADR-001). " +
            $"Violating types: {string.Join(", ", failingTypes)}");
    }
}
