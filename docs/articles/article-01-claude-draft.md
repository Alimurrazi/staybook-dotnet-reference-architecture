# Designing and setting up Staybook

*Staybook, part 1 of 14 · Code: tag [`article-01`](https://github.com/Alimurrazi/staybook-dotnet-reference-architecture/tree/article-01) · Full details: [article 1 report](../reports/article-01-designing-and-setting-up-staybook.md)*

Most architecture tutorials pick their patterns first and find a domain later. This series does the opposite. We build **Staybook**, an Airbnb-style vacation rental backend in ASP.NET Core 10, and add each pattern only when the domain demands it: event sourcing when bookings need a history, an outbox when we lose our first message, sagas when a host gets 24 hours to answer.

This first article designs Staybook and sets up a solution you can clone, run and test, with its boundaries enforced from the first commit.

## Why a rental platform

Everyone knows how it works, so we can skip the requirements and go straight to the hard parts:

- **Availability:** two guests book the same nights at the same moment. Exactly one must win.
- **Money:** every cent must add up, and a price a guest was quoted must never change.
- **Time:** a booking request waits up to 24 hours while the nights are on hold and the card is authorized.
- **Failure:** the payment provider times out. Did the charge happen? A timeout is not a failure; it's an *unknown*.

Everything else is deliberately small. No photos, no chat, no wishlists, no frontend. Every feature has to teach something.

Staybook is a **production-oriented reference architecture**, not a platform you could run a business on. No real money moves, there are no backups, and nothing has had a legal or security audit. Saying so up front keeps the rest honest.

## Finding the boundaries

Instead of drawing boxes, walk through one scenario: *a guest requests a stay, the host accepts, the payment is captured.* Watch where the vocabulary changes:

1. The guest finds a **listing**: details, a lifecycle, draft or published.
2. They ask for a price. Rates, fees, discounts, rounding and a stored, immutable **quote** are a different language that changes for different reasons. That's **pricing**.
3. They request to book with the quote's ID (never a price). The nights go on **hold** (**availability**), the card is **authorized** (**payments**), and the request is recorded (**booking**).
4. The host accepts: the hold becomes a confirmed allocation, then the money is captured.
5. Both get an email (**notifications**), and someone is always a guest or a host (**identity**).

Each shift in language is a boundary. That gives seven modules. Only three exist today (Listings, Pricing and Identity), because the others arrive in the articles that need them.

## Three ways to talk

Modules communicate in exactly three ways:

| Form | When |
|---|---|
| **Query** through the other module's contracts | Reading its data |
| **Command** through its contracts | **Only** for steps a user is waiting for, always with an operation ID |
| **Message** | Everything else |

The middle row is the one that spreads if you let it. Staybook allows exactly two synchronous commands: allocating nights and taking the payment. Everything else is a message. Because a call can succeed while its response is lost, each command carries an operation ID, so a retry returns the original result instead of doing the work twice.

## Not everything has to be right immediately

Allocating nights must be **immediately and absolutely** correct; a database constraint enforces it. Search results and emails can be a second late. And money can never be atomic with an external provider, so the design records the intent first and resolves the outcome after.

Two rules come out of that, and they hold for the whole series: **a view never decides availability or money**, and **a provider timeout is never a failure**.

## A modular monolith

Seven modules sound like seven microservices. They aren't, yet. Staybook is one application with one PostgreSQL database and hard boundaries inside it:

- every module owns its own schema, and no module reads another's tables;
- modules reference each other only through **Contracts** projects;
- **architecture tests** fail the test run when a boundary is crossed.

Microservices would turn every boundary into a network call and a deployment before we know the boundaries are right. When there's a real reason to split, in article 11, the boundaries will already be there.

Each decision has a one-page record in `docs/adr/`: the modular monolith, the project structure, Wolverine instead of MediatR, Marten on PostgreSQL, and logging through `ILogger` with OpenTelemetry.

## The setup, in decisions

```
src/
  Staybook.AppHost/          Aspire: PostgreSQL, Keycloak, the API
  Staybook.ServiceDefaults/  OpenTelemetry and health checks
  Staybook.Api/              Composition root, no business logic
  Staybook.SharedKernel/     Shared types, kept small
  Modules/<Module>/          Staybook.<Module> + Staybook.<Module>.Contracts
tests/Staybook.ArchitectureTests/
```

- **A strict build.** Warnings are errors, recommended analyzers are on, style rules run in the build, and package versions live in one file.
- **One command to run it.** `dotnet run --project src/Staybook.AppHost` starts PostgreSQL and Keycloak in Docker, wires the connection string and opens a dashboard with logs, traces and metrics.
- **Registered, not configured.** Marten and Wolverine are registered, but nothing uses them until article 3. Infrastructure arrives when it pays for itself.

Running the app, not just building it, mattered: the API compiled fine but crashed at startup, because Wolverine 6 ships its runtime compiler as a separate package. A passing build proves the code compiles, not that the application starts.

## Tests that check themselves

Article 1 has no business logic, but it has **37 architecture tests**: modules use each other only through contracts, the domain uses no framework, layers point inward, and the shared kernel depends on nothing.

The interesting part is a bug. ArchUnitNET, the library behind these tests, loses every dependency inside an `async` method in **Release** builds. Our handlers will be async, so every boundary test would pass while checking nothing. So the tests check themselves:

- a **canary** hides a forbidden dependency inside an async method, and a test proves it's detected;
- a guard fails if the tests run against an optimized build.

In Debug, all 37 pass. In Release, the guard and the canary fail, exactly as they should.

A second surprise came from review. Reading a `const` from another module leaves no trace in the compiled code, because the compiler copies the value. The type rules can't see that, so one more test reads the project files and rejects forbidden references directly. The type rules catch what the code *uses*; that test catches what the project *may* use.

## Built with Claude Code

Staybook is built with Claude Code, and the harness is part of the repo:

- **`CLAUDE.md` files**, written before any code, state the rules in plain words: one at the root, one per module.
- **Hooks**, written in C#, block edits to generated code and secrets, build after every edit, and run the affected tests before Claude finishes.
- **Skills** scaffold value objects, aggregates, commands and endpoints with the rules built in.
- **Subagents:** a test writer that works test-first, and an architecture reviewer that went through this whole article's branch before the author did.

The idea in one line: **`CLAUDE.md` guides; tests, analyzers and hooks enforce.** One lesson along the way: two hooks that asked for commits were removed, because a hook can't tell whose changes are uncommitted. A hook should only enforce what it can judge correctly every time.

## Try it

You need the .NET 10 SDK and Docker.

```bash
git clone https://github.com/Alimurrazi/staybook-dotnet-reference-architecture.git
cd staybook-dotnet-reference-architecture
git checkout article-01
dotnet test --solution Staybook.slnx
dotnet run --project src/Staybook.AppHost
```

37 tests pass, and `http://localhost:5174/health` answers `Healthy`. Then break something: reference `Staybook.Pricing` from Listings, use one of its types, and watch the tests name the violation.

**Next:** article 2 models the domain: money that never loses a cent, a listing with a real lifecycle, and quotes that never change. Pure C#, no database, every rule a test.
