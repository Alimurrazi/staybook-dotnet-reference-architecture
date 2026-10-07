# Staybook

An Airbnb-style vacation rental **backend** in ASP.NET Core 10, built step by step in a 14-article series.

Staybook is a **production-oriented reference architecture**: it shows how a simple rental application evolves when it meets real problems (concurrency, consistency, external failures, financial correctness and security), and which architectural decisions those problems justify. Every pattern arrives in the article whose problem needs it, with the trade-offs and the alternatives.

> Staybook is inspired by Airbnb. It isn't affiliated with Airbnb, and it uses none of its branding.

## Where this tag stands

**Tag `article-01`: Designing and setting up Staybook.**

- A modular monolith with three module skeletons: Listings, Pricing and Identity
- PostgreSQL through .NET Aspire; Marten and Wolverine registered; Keycloak registered for article 4
- OpenTelemetry to the Aspire dashboard; health checks
- Architecture tests enforcing module boundaries and layers
- A Claude Code harness: `CLAUDE.md` files, permissions, hooks, skills and subagents
- ADRs 1 to 4

Read the article: [`docs/articles/01-designing-and-setting-up-staybook.md`](docs/articles/01-designing-and-setting-up-staybook.md).

## Prerequisites

| Tool | Version | Why |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0.100 or later | Builds and runs everything |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or another Docker engine) | Any recent version | Aspire runs PostgreSQL and Keycloak as containers |
| [Claude Code](https://claude.com/claude-code) | Optional | Only if you want to work with the harness in `.claude/` |

No database, Keycloak or Aspire CLI to install: Aspire pulls everything as containers and NuGet packages.

## Run it

```bash
git clone https://github.com/Alimurrazi/staybook-dotnet-reference-architecture.git
cd staybook-dotnet-reference-architecture
git checkout article-01

dotnet build Staybook.slnx
dotnet test --solution Staybook.slnx
dotnet run --project src/Staybook.AppHost
```

The first run pulls the PostgreSQL and Keycloak images, which takes a few minutes. Then:

| What | Where | Expected |
|---|---|---|
| Aspire dashboard | The **login URL** printed in the console (`http://localhost:15174/login?t=…`) | Resources `postgres`, `staybook`, `keycloak` and `api`, all running |
| API health | `http://localhost:5174/health` | `Healthy` (includes the PostgreSQL check) |
| API liveness | `http://localhost:5174/alive` | `Healthy` |

Stop with `Ctrl+C`. Data is kept in the Docker volumes `staybook-postgres-data` and `staybook-keycloak-data`; remove them with `docker volume rm` to start fresh.

## Tests

```bash
dotnet test --solution Staybook.slnx                        # everything
dotnet test --project tests/Staybook.ArchitectureTests      # architecture tests only
```

Run tests in the default **Debug** configuration. Two architecture tests fail on purpose in Release builds, because ArchUnitNET misses dependencies inside `async` methods there ([TNG/ArchUnitNET#498](https://github.com/TNG/ArchUnitNET/issues/498)); see ADR 2.

## Repository layout

```
src/
  Staybook.AppHost/          Aspire: starts PostgreSQL, Keycloak and the API
  Staybook.ServiceDefaults/  OpenTelemetry, health checks
  Staybook.Api/              Host and composition root
  Staybook.SharedKernel/     Shared types (filled from article 2)
  Modules/<Module>/          Staybook.<Module> and Staybook.<Module>.Contracts, plus the module's CLAUDE.md
tests/
  Staybook.ArchitectureTests/
docs/
  adr/                       Architecture Decision Records
  articles/                  The article text for each tag
  architecture.md            Context map and the evolving architecture diagram
  planning/                  The series plan and the reviews that shaped it
.claude/                     Claude Code harness: settings, hooks, skills, subagents
```

## Known limitations of `article-01`

- **No features yet.** There are no endpoints besides `/health` and `/alive`. Listings and pricing arrive in articles 2 and 3.
- **No authentication.** Keycloak runs but isn't configured; the API doesn't validate tokens until article 4.
- **Health endpoints are mapped only in Development.** Exposing them elsewhere is a deployment decision.
- **No CI workflow yet.** Builds and tests run locally (and through the Claude Code hooks). CI is a planned follow-up.
- **Keycloak's Aspire integration is a preview package** (`Aspire.Hosting.Keycloak` 13.6.1-preview).
- **Local development only.** Deployment is the subject of a separate series.

## The series

| Phase | Articles |
|---|---|
| 1. Build a secure foundation | 1 Designing and setting up · 2 Domain modeling with DDD · 3 CQRS and the application layer · 4 Authentication and authorization |
| 2. Solve the booking problem | 5 Event sourcing with Marten · 6 Availability and concurrency · 7 Reliable messaging with Wolverine · 8 Reliable booking sagas |
| 3. Handle money and distributed failures | 9 Integrating Stripe safely · 10 Financial correctness, cancellations and reconciliation · 11 Distributed reliability and evolving the system |
| 4. Harden and evaluate the system | 12 Production-oriented API design · 13 Securing workflows · 14 End-to-end verification and retrospective |

Each article has a git tag (`article-01` to `article-14`), and each phase a GitHub release.

## What this project does not guarantee

Staybook is a reference architecture, not a platform to run a business on:

- **No real money movement.** Stripe runs in test mode; payouts are simulated.
- **No operational recovery.** No production deployment, backups or tested restores.
- **No regulatory compliance.** GDPR is discussed, but there is no legal review.
- **No security certification.** The threat model and tests reduce risk; they don't replace a security audit.
- **No scale validation** beyond the load tests the series shows.

## Credits

The series started from a review of [jeangatto/ASP.NET-Core-Clean-Architecture-CQRS-Event-Sourcing](https://github.com/jeangatto/ASP.NET-Core-Clean-Architecture-CQRS-Event-Sourcing), a good Clean Architecture and CQRS sample whose gaps (event sourcing as an audit log, events published after commit) became teaching examples for later articles.
