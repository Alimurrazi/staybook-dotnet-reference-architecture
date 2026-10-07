using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using Assembly = System.Reflection.Assembly;

namespace Staybook.ArchitectureTests;

/// <summary>
/// The assemblies the architecture tests check, loaded once for all tests.
/// </summary>
internal static class StaybookArchitecture
{
    /// <summary>Every module under src/Modules. A test fails if a folder is missing here.</summary>
    public static IReadOnlyList<string> Modules { get; } = ["Identity", "Listings", "Pricing"];

    /// <summary>
    /// Namespaces of frameworks that belong in Infrastructure or Endpoints, never in Domain,
    /// Application or the shared kernel.
    /// </summary>
    public static IReadOnlyList<string> InfrastructureFrameworks { get; } =
        ["Marten", "Wolverine", "Npgsql", "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "JasperFx"];

    public static Assembly SharedKernel { get; } = Assembly.Load("Staybook.SharedKernel");

    public static IReadOnlyList<Assembly> Assemblies { get; } =
    [
        SharedKernel,
        .. Modules.SelectMany(module => new[] { ModuleAssembly(module), ContractsAssembly(module) }),
    ];

    public static Architecture Architecture { get; } =
        new ArchLoader().LoadAssemblies([.. Assemblies]).Build();

    public static Assembly ModuleAssembly(string module) => Assembly.Load($"Staybook.{module}");

    public static Assembly ContractsAssembly(string module) => Assembly.Load($"Staybook.{module}.Contracts");

    /// <summary>A namespace and everything below it, as a regular expression.</summary>
    public static string NamespaceAndBelow(string root) => $@"^{root.Replace(".", @"\.", StringComparison.Ordinal)}(\..+)?$";
}
