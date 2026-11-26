using System.Reflection;
using NetArchTest.Rules;

namespace Invoicing.ArchitectureTests;

/// <summary>
/// Enforces the API boundary (ADR-001 Decision 4, docs/architecture/architecture.md API-3, API-4, API-5):
/// Api delegates to Application, never accesses persistence directly, and does not use Domain types.
/// </summary>
/// <remarks>
/// API-5 allows Api to reference Infrastructure only for composition in Program.cs. The generated
/// Program class lives in the global namespace, so "outside the composition root" is expressed as
/// types residing in the Invoicing.Api namespaces.
/// </remarks>
public class ApiBoundaryTests
{
    private const string ApiNamespace = "Invoicing.Api";

    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    // ADR-001 Decision 4 ("must not directly access persistence"), API-3, API-5.
    [Fact]
    public void Api_should_not_depend_on_Infrastructure_outside_composition_root() =>
        AssertNoDependency(ApiTypesOutsideCompositionRoot(), "Invoicing.Infrastructure");

    // ADR-001 Decision 4, API-3 ("no DbContext"), PER-4.
    [Fact]
    public void Api_should_not_depend_on_EntityFrameworkCore_outside_composition_root() =>
        AssertNoDependency(ApiTypesOutsideCompositionRoot(), "Microsoft.EntityFrameworkCore");

    // ADR-001 Decision 4, API-3 ("no ... repositories or unit of work in endpoints"), PER-1.
    [Theory]
    [InlineData("Invoicing.Application.IInvoiceRepository")]
    [InlineData("Invoicing.Application.ICustomerRepository")]
    [InlineData("Invoicing.Application.IUnitOfWork")]
    public void Api_should_not_use_persistence_ports_outside_composition_root(string persistencePort) =>
        AssertNoDependency(ApiTypesOutsideCompositionRoot(), persistencePort);

    // architecture.md §4 footnote (1) and API-4, referenced by ADR-001 ("detailed rules are in §4").
    [Fact]
    public void Api_should_not_depend_on_Domain() =>
        AssertNoDependency(Types.InAssembly(ApiAssembly), "Invoicing.Domain");

    private static PredicateList ApiTypesOutsideCompositionRoot() =>
        Types.InAssembly(ApiAssembly).That().ResideInNamespaceStartingWith(ApiNamespace);

    private static void AssertNoDependency(Types types, string forbiddenDependency) =>
        AssertResult(types.ShouldNot().HaveDependencyOn(forbiddenDependency).GetResult(), forbiddenDependency);

    private static void AssertNoDependency(PredicateList types, string forbiddenDependency) =>
        AssertResult(types.ShouldNot().HaveDependencyOn(forbiddenDependency).GetResult(), forbiddenDependency);

    private static void AssertResult(TestResult result, string forbiddenDependency)
    {
        var failingTypes = result.FailingTypeNames ?? [];
        Assert.True(result.IsSuccessful,
            $"Invoicing.Api must not depend on {forbiddenDependency} (ADR-001). " +
            $"Violating types: {string.Join(", ", failingTypes)}");
    }
}
