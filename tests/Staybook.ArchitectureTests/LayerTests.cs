using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static Staybook.ArchitectureTests.StaybookArchitecture;

namespace Staybook.ArchitectureTests;

/// <summary>
/// The dependency rule inside a module: Endpoints → Application → Domain, and
/// Infrastructure → Application, Domain. The domain stays free of frameworks.
/// </summary>
/// <remarks>
/// Most layers are still empty in article 1, so these rules don't require matching types
/// yet. The canary test in <see cref="BuildConfigurationTests"/> proves they detect
/// violations once code arrives.
/// </remarks>
public class LayerTests
{
    public static TheoryData<string> ModuleNames => [.. Modules];

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Domain_depends_on_no_other_layer(string module)
    {
        Types().That().ResideInNamespaceMatching(NamespaceAndBelow($"Staybook.{module}.Domain"))
            .Should().NotDependOnAny(Types().That()
                .ResideInNamespaceMatching(NamespaceAndBelow($"Staybook.{module}.Application"))
                .Or().ResideInNamespaceMatching(NamespaceAndBelow($"Staybook.{module}.Infrastructure"))
                .Or().ResideInNamespaceMatching(NamespaceAndBelow($"Staybook.{module}.Endpoints")))
            .Because("the domain is the inner circle; everything else depends on it, never the reverse")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Application_depends_on_neither_infrastructure_nor_endpoints(string module)
    {
        Types().That().ResideInNamespaceMatching(NamespaceAndBelow($"Staybook.{module}.Application"))
            .Should().NotDependOnAny(Types().That()
                .ResideInNamespaceMatching(NamespaceAndBelow($"Staybook.{module}.Infrastructure"))
                .Or().ResideInNamespaceMatching(NamespaceAndBelow($"Staybook.{module}.Endpoints")))
            .Because("use cases depend on abstractions; Infrastructure and Endpoints plug into them")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Domain_depends_on_no_infrastructure_framework(string module)
    {
        Types().That().ResideInNamespaceMatching(NamespaceAndBelow($"Staybook.{module}.Domain"))
            .Should().NotDependOnAny(InfrastructureFrameworkTypes())
            .Because("the domain is plain C#: no persistence, messaging or HTTP")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Fact]
    public void Shared_kernel_depends_on_no_module_and_no_infrastructure_framework()
    {
        var moduleTypes = Types().That().ResideInAssembly(ModuleAssembly(Modules[0]));
        foreach (var module in Modules.Skip(1))
        {
            moduleTypes = moduleTypes.Or().ResideInAssembly(ModuleAssembly(module));
        }

        Types().That().ResideInAssembly(SharedKernel)
            .Should().NotDependOnAny(moduleTypes)
            .AndShould().NotDependOnAny(InfrastructureFrameworkTypes())
            .Because("every module depends on the shared kernel, so it must depend on nothing that changes often")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    private static ArchUnitNET.Fluent.Syntax.Elements.Types.GivenTypesConjunction InfrastructureFrameworkTypes()
    {
        var types = Types(true).That().ResideInNamespaceMatching(NamespaceAndBelow(InfrastructureFrameworks[0]));
        foreach (var framework in InfrastructureFrameworks.Skip(1))
        {
            types = types.Or().ResideInNamespaceMatching(NamespaceAndBelow(framework));
        }

        return types;
    }
}
