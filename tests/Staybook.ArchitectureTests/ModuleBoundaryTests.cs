using System.Reflection;
using ArchUnitNET.xUnitV3;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static Staybook.ArchitectureTests.StaybookArchitecture;

namespace Staybook.ArchitectureTests;

/// <summary>
/// Modules talk to each other only through Contracts projects, and each owns its schema
/// (plan section 7).
/// </summary>
public class ModuleBoundaryTests
{
    public static TheoryData<string> ModuleNames => [.. Modules];

    public static TheoryData<string, string> ModulePairs =>
        [.. Modules.SelectMany(module => Modules.Where(other => other != module).Select(other => (module, other)))];

    [Fact]
    public void Every_module_folder_is_checked_by_the_architecture_tests()
    {
        var folders = Directory.GetDirectories(Path.Combine(RepositoryRoot(), "src", "Modules"))
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal);

        folders.ShouldBe(Modules.Order(StringComparer.Ordinal),
            "Add the new module to StaybookArchitecture.Modules and reference its projects from this test project.");
    }

    [Theory]
    [MemberData(nameof(ModulePairs))]
    public void A_module_uses_another_module_only_through_its_contracts(string module, string other)
    {
        Types().That().ResideInAssembly(ModuleAssembly(module))
            .Should().NotDependOnAny(Types().That().ResideInAssembly(ModuleAssembly(other)))
            .Because($"{module} may use {other} only through Staybook.{other}.Contracts")
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(ModulePairs))]
    public void Contracts_never_expose_another_module_implementation(string module, string other)
    {
        Types().That().ResideInAssembly(ContractsAssembly(module))
            .Should().NotDependOnAny(Types().That().ResideInAssembly(ModuleAssembly(other)))
            .Because("contracts hold IDs, DTOs, interfaces and messages, never another module's internals")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Contracts_never_depend_on_their_own_module_implementation(string module)
    {
        Types().That().ResideInAssembly(ContractsAssembly(module))
            .Should().NotDependOnAny(Types().That().ResideInAssembly(ModuleAssembly(module)))
            .Because("other modules reference the contracts; domain types must not leak through them")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Each_module_owns_a_schema_named_after_it(string module)
    {
        var registration = ModuleAssembly(module).GetType($"Staybook.{module}.{module}Module");
        registration.ShouldNotBeNull($"Staybook.{module} needs a {module}Module registration class.");

        var schema = registration.GetField("Schema", BindingFlags.Public | BindingFlags.Static)?.GetRawConstantValue();

        schema.ShouldBe(module.ToLowerInvariant(),
            "Each module owns one PostgreSQL schema, named after the module, so no two modules share tables.");
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Staybook.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Staybook.slnx not found above the test output folder.");
    }
}
