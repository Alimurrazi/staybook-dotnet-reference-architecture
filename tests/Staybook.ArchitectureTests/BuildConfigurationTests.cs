using System.Diagnostics;
using System.Reflection;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Shouldly;
using Staybook.ArchitectureTests.Canary;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Staybook.ArchitectureTests;

/// <summary>
/// Architecture tests that pass for the wrong reason are worse than none. These two tests
/// make sure the other tests can actually see the code they check.
/// </summary>
public class BuildConfigurationTests
{
    [Fact]
    public void Analyzed_assemblies_are_built_without_optimizations()
    {
        var optimized = StaybookArchitecture.Assemblies
            .Append(typeof(BuildConfigurationTests).Assembly)
            .Where(assembly => assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled != true)
            .Select(assembly => assembly.GetName().Name);

        optimized.ShouldBeEmpty(
            "ArchUnitNET misses dependencies inside async methods in optimized (Release) builds "
            + "(TNG/ArchUnitNET#498), so negative rules would pass silently. Run the architecture tests in Debug.");
    }

    [Fact]
    public void A_dependency_inside_an_async_method_is_detected()
    {
        var architecture = new ArchLoader().LoadAssemblies(typeof(AsyncCanary).Assembly).Build();

        // A positive rule, on purpose: it passes only if AsyncCanary is found AND its
        // dependency is seen. Asserting that a negative rule fails would also "succeed"
        // if the canary type stopped matching at all.
        Types().That().Are(typeof(AsyncCanary))
            .Should().DependOnAny(Types().That().Are(typeof(ForbiddenDependency)))
            .Because("AsyncCanary uses ForbiddenDependency inside an async method. If this isn't seen, "
                + "the architecture tests can't see async code and every boundary rule is unreliable")
            .Check(architecture);
    }
}
