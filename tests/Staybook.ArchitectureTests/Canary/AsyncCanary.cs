namespace Staybook.ArchitectureTests.Canary;

// A deliberate dependency hidden inside an async method, so the architecture tests can
// prove they still see dependencies there. See BuildConfigurationTests.

public static class ForbiddenDependency
{
    public static int Value => 42;
}

public static class AsyncCanary
{
    // The compiler moves this body into a state machine. ArchUnitNET finds it in Debug
    // builds but loses it in Release builds (TNG/ArchUnitNET#498).
    public static async Task<int> UseForbiddenDependencyAsync()
    {
        await Task.Yield();
        return ForbiddenDependency.Value;
    }
}
