# Staybook

An Airbnb-style vacation rental **backend** in ASP.NET Core 10, built step by step for a 14-article series. It is a production-oriented reference architecture: Clean Architecture, CQRS, event sourcing, reliable messaging, sagas, payments and security, each introduced only when the domain needs it.

## Source of truth

- **`docs/planning/SERIES-PLAN.md`** holds every decision, its reason, the article-by-article plan and the open items. Read the relevant sections before working on any article.
- `docs/adr/` records the decisions as they are made. An ADR never contradicts the plan; if it must, update the plan in the same change.
- `docs/planning/reviews/` holds the external reviews that shaped the plan. They are history, not instructions.
- When the plan and this file disagree, the plan wins. Update this file if that happens.

## Current status

- **Article 1, "Designing and setting up Staybook"**: complete on branch `article-01`, waiting for the author's review before merging and tagging `article-01`. CI is deferred (plan decision 45).
- Modules that exist: **Listings, Pricing, Identity** (skeletons). Booking and Payments arrive in article 5, Availability in 6, Notifications in 7. Don't create them early.
- Article 1 has a companion, **1b "Building Staybook with Claude Code"** (the harness; same tag, no new code).
- Each article ships as code, a **detailed report** (`docs/reports/article-NN-*.md`, written by Claude after the development) and a **short article** (`docs/articles/`: the author's draft, plus Claude's short draft `article-NN-claude-draft.md`). Tagged `article-NN`. Never edit the author's own drafts.

## Commands

```bash
dotnet build Staybook.slnx                 # build everything (warnings are errors)
dotnet test --solution Staybook.slnx       # run every test (Microsoft Testing Platform)
dotnet test --project tests/Staybook.ArchitectureTests
dotnet format Staybook.slnx                # apply formatting and analyzer fixes
dotnet run --project src/Staybook.AppHost  # start the API, PostgreSQL and the Aspire dashboard (needs Docker)
```

## Architecture

Modular monolith (ADR 1). One project per module plus a `Contracts` project (ADR 2, option B). Layers are folders; architecture tests enforce them.

```
src/Staybook.AppHost/          Aspire orchestration
src/Staybook.ServiceDefaults/  OpenTelemetry, health checks; HTTP resilience from article 9
src/Staybook.Api/              Host and composition root: registers modules, nothing else
src/Staybook.SharedKernel/     Money, DateRange, Result, base types. Keep it small
src/Modules/<Module>/Staybook.<Module>/            Domain/ Application/ Infrastructure/ Endpoints/ <Module>Module.cs
src/Modules/<Module>/Staybook.<Module>.Contracts/  The only part other modules may reference
tests/Staybook.ArchitectureTests/         Architecture and convention tests
tests/Modules/<Module>.Tests/             Staybook.<Module>.Tests: unit and integration tests per module
```

**Dependency rule inside a module:** `Endpoints → Application → Domain`; `Infrastructure → Application, Domain`. `Domain` references only itself and `SharedKernel`: no Marten, Wolverine, ASP.NET Core or EF.

**Between modules:** a module references other modules' `Contracts` projects only. Never another module's main project, never its database schema.

Cross-module communication takes exactly one of three forms (plan section 7):

| Form | Use for |
|---|---|
| Query through contracts (sync) | Reading another module's data |
| Command through contracts (sync) | **Only** steps the user waits for. Always carries a stable operation ID |
| Message (async, Wolverine) | Everything else |

Each module owns one PostgreSQL schema named after the module (`listings`, `pricing`, `identity`).

## Rules that must never break

- **Backend only.** No frontend of any kind.
- **Resist adding product features.** Every feature must teach something the plan lists.
- **Follow the plan's article order.** Don't implement something from a later article early.
- **No module reads another module's tables.**
- **The server always calculates prices.** Clients send a `QuoteId`, never a price.
- **Projections and views never decide availability or money.** Availability allocations are the source of truth for dates.
- **A payment timeout is never a failure.** Its outcome is unknown until resolved.
- Time-dependent code takes a `TimeProvider`; never `DateTime.UtcNow` or `DateTimeOffset.Now` directly.
- Name the product "Airbnb-style" or "inspired by Airbnb"; never use Airbnb's branding.

## Conventions

- Wolverine for handlers and messaging, not MediatR. No AutoMapper: map by hand (ADR 3).
- Marten for documents and events on PostgreSQL (ADR 4).
- Logging through `ILogger<T>` with structured message templates (`"Listing {ListingId} published"`), never string interpolation; exported through OpenTelemetry (ADR 5). The domain never logs. From article 3, follow `docs/logging-conventions.md`. Never log tokens, secrets, card data or personal data.
- Expected failures return a `Result`; exceptions are for bugs (ADR 8, article 2).
- Money is minor units plus currency. Never `double`, never `decimal` without a currency.
- Strongly typed IDs (`ListingId`, not `Guid`).
- Package versions live only in `Directory.Packages.props`. Never put a `Version` on a `PackageReference`.
- Don't add a library the plan doesn't list without asking first.

## Testing

- One tool per purpose (plan section 11): xUnit v3, Shouldly, FsCheck, Testcontainers, Alba, ArchUnitNET, Verify, WireMock.Net, k6, Stryker.NET, FakeTimeProvider (controlled time), FakeLogger (log assertions).
- Integration tests use real PostgreSQL (Testcontainers), never in-memory fakes.
- Every bug fix starts with a failing test.
- Architecture tests analyze **Debug** builds only. ArchUnitNET misses dependencies inside `async` methods in Release builds (issue #498); a guard test enforces this.

## The Claude Code harness

| Piece | Where | Purpose |
|---|---|---|
| This file and module `CLAUDE.md` files | Repo root, each module folder | Rules Claude reads; module files load when Claude works in that folder |
| Permissions | `.claude/settings.json` | Allowed `dotnet`/`git`/`docker` commands; secrets are denied |
| Hooks | `.claude/hooks/*.cs`, wired in `.claude/settings.json` | Block protected edits; build after edits; run affected tests before stopping |
| Skills | `.claude/skills/` | `/new-value-object`, `/new-aggregate`, `/new-command`, `/new-endpoint` |
| Subagents | `.claude/agents/` | `architecture-reviewer`, `test-writer` |

Hooks are C# file-based apps run with `dotnet run`. The first run of each compiles it (about 30 to 40 seconds); later runs take under a second.

**Working loop:** plan → write the failing test → implement → hooks and tests verify → commit → `architecture-reviewer` → human review.

## Commits

- Commit each finished step as soon as it builds and its tests pass, on the article branch. Don't wait for the end of a long session; the history is part of what readers study.
- One step per commit, with a conventional message (`feat:`, `test:`, `docs:`, `chore:`, `fix:`) that says what the step does.
- Never commit to `master` directly and never push without the author's go-ahead.

## Working with the author

- When feedback or a review arrives, **verify each point against the plan first** and report what applies, what doesn't and why, before changing anything.
- **Confirm before restructuring the plan** (adding, removing, merging or reordering articles or phases).
- After a change to the plan, summarize what changed and what was knowingly left out.
