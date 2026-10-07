# ADR 2: One project per module plus a Contracts project

- **Status:** Accepted
- **Date:** 2026-10-07
- **Article:** 1

## Context

ADR 1 makes modules the main boundary. Inside each module, Clean Architecture asks for layers with a dependency rule: the domain depends on nothing, the application layer depends on the domain, and infrastructure and endpoints plug in from the outside.

The structure has to make the module boundary obvious, keep the layering honest, and not drown a reader in projects.

## Decision

**Option B from the plan:** each module has two projects.

```
src/Modules/<Module>/
  Staybook.<Module>/              Domain/  Application/  Infrastructure/  Endpoints/  <Module>Module.cs
  Staybook.<Module>.Contracts/    IDs, DTOs, query and command interfaces, messages
```

- **Layers are folders** (and namespaces) inside the module project. The dependency rule between them is enforced by architecture tests, not by project references.
- **Contracts are a separate project**, which makes the most important rule visible in every project file: another module may reference only `Staybook.<Module>.Contracts`. The compiler doesn't enforce it (a reference to another module's main project compiles), so the architecture tests do, twice: one test reads the `ProjectReference`s of every module project, and the type rules check what the code actually uses. Both are needed: a `const` read through a forbidden reference is inlined by the compiler and leaves no type dependency to find.
- `<Module>Module.cs` is the module's only entry point for the host: `Add<Module>Module()` and `Map<Module>Endpoints()`. It also names the schema the module owns.
- Shared types go in `Staybook.SharedKernel`, kept deliberately small.
- Architecture tests (ArchUnitNET) check module boundaries, layers and the shared kernel. Layers are found by namespace, so namespaces must match folders (`IDE0130` fails the build). They run against **Debug builds only**: ArchUnitNET misses dependencies inside `async` methods in Release builds (TNG/ArchUnitNET#498), which would let negative rules pass silently. A guard test fails if the analyzed assemblies are optimized, and a canary test proves an async dependency is detected.

## Alternatives considered

| Alternative | Trade-off |
|---|---|
| **Option A:** projects per layer per module (`Booking.Domain`, `Booking.Application`, `Booking.Infrastructure`, `Booking.Contracts`) | The compiler enforces the layers too, which is a real benefit. But seven modules × four projects is 28 projects plus hosts and tests, for a codebase whose modules are small. Most of the benefit is available from architecture tests at a fraction of the noise |
| One project per layer for the whole solution (`Staybook.Domain`, `Staybook.Application`, …) | The classic Clean Architecture template. It hides the module boundaries, which ADR 1 makes the most important ones, and every module change touches every project |
| No Contracts projects; modules reference each other's main project | Nothing would stop one module from using another's domain types or tables |

## Consequences

- 2 projects per module instead of 4: the solution stays readable.
- A layer violation inside a module (for example `Domain` using Marten) compiles. It fails the architecture tests instead, so those tests must run on every change. The Claude Code Stop hook runs them before any change is reported done; a CI workflow is a follow-up (plan decision 45).
- Architecture tests that silently pass are worse than none. The Debug-only rule and the canary test exist because of that.
- If a module grows enough that its layers need compiler enforcement, it can be split into option A projects without affecting other modules, because they only see its Contracts.
