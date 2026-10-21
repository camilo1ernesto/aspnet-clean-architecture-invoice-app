using System.Reflection;
using NetArchTest.Rules;

namespace Invoicing.ArchitectureTests;

/// <summary>
/// Enforces the dependency direction in ADR-001 and docs/architecture/architecture.md §4:
/// dependencies point inward (Api → Application/Infrastructure → Domain) and never outward.
/// </summary>
public class LayerDependencyTests
{
    private const string ApplicationNamespace = "Invoicing.Application";
    private const string InfrastructureNamespace = "Invoicing.Infrastructure";
    private const string ApiNamespace = "Invoicing.Api";

    private static readonly Assembly DomainAssembly = typeof(Domain.Abstractions.BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;

    // ADR-001 Decision 1, architecture.md DOM-1.
    [Fact]
    public void Domain_should_not_depend_on_Infrastructure() =>
        AssertNoDependency(DomainAssembly, InfrastructureNamespace);

    // ADR-001 Decision 1, architecture.md DOM-1.
    [Fact]
    public void Domain_should_not_depend_on_Application() =>
        AssertNoDependency(DomainAssembly, ApplicationNamespace);

    // ADR-001 Decision 1, architecture.md DOM-1.
    [Fact]
    public void Domain_should_not_depend_on_Api() =>
        AssertNoDependency(DomainAssembly, ApiNamespace);

    // ADR-001 Decisions 2 and 5, architecture.md APP-1 and APP-2.
    [Fact]
    public void Application_should_not_depend_on_Infrastructure() =>
        AssertNoDependency(ApplicationAssembly, InfrastructureNamespace);

    // ADR-001 Decisions 2 and 5, architecture.md APP-1.
    [Fact]
    public void Application_should_not_depend_on_Api() =>
        AssertNoDependency(ApplicationAssembly, ApiNamespace);

    // ADR-001 Decisions 3 and 5, architecture.md INF-1.
    [Fact]
    public void Infrastructure_should_not_depend_on_Api() =>
        AssertNoDependency(InfrastructureAssembly, ApiNamespace);

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
