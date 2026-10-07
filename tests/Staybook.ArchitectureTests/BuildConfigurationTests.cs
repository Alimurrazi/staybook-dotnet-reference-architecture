using System.Diagnostics;
using System.Reflection;
using ArchUnitNET.Loader;
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
        var rule = Types().That().Are(typeof(AsyncCanary))
            .Should().NotDependOnAny(Types().That().Are(typeof(ForbiddenDependency)));

        rule.HasNoViolations(architecture).ShouldBeFalse(
            "AsyncCanary depends on ForbiddenDependency inside an async method. If this rule passes, "
            + "the architecture tests can't see async code and every boundary rule is unreliable.");
    }
}
