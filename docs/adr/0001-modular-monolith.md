# ADR 1: Modular monolith over microservices

- **Status:** Accepted
- **Date:** 2026-10-07
- **Article:** 1

## Context

Staybook has six business capabilities with clear seams: Listings, Pricing, Identity, Booking, Availability and Payments (Notifications arrives in article 7). Some operations must be immediately consistent across two of them: a guest who books waits while Booking allocates nights in Availability and authorizes the payment in Payments. Others can be eventual: search, notifications.

The team is one person, there is one deployment target, and the load is what an educational project generates. The boundaries are the hardest part to get right and the most expensive to move later.

## Decision

Build a **modular monolith**: one deployable ASP.NET Core application, one PostgreSQL database, and modules with hard boundaries inside it.

- Each module owns a PostgreSQL schema. No module reads another module's tables.
- Modules communicate in exactly three ways (plan section 7): a query through the other module's contracts, a command through its contracts (only for steps the user waits for, with a stable operation ID), or an asynchronous message.
- Architecture tests enforce the boundaries from the first commit (ADR 2).
- One module is extracted into its own service in article 11, when there is a concrete reason to show what extraction costs.

## Alternatives considered

| Alternative | Why not now |
|---|---|
| Microservices from the start | Every boundary becomes a network call, a deployment and a failure mode before we know the boundaries are right. Allocation plus payment authorization would need distributed coordination from day one. The operational cost would dominate a series about application architecture |
| A traditional layered monolith (one project, no module boundaries) | Cheap to start, but nothing stops Pricing code from reading Booking tables. The boundaries would erode, and article 11's extraction would become a rewrite |
| One database per module inside the monolith | Stronger isolation, but loses single-transaction guarantees the design relies on (the outbox in article 7 shares the module's transaction). Schema per module gives the same ownership rule with one database to run |

## Consequences

- One process to run, debug and test; Aspire starts it with PostgreSQL in one command.
- Modules can be extracted later because they already talk through contracts and messages. Article 11 tests this claim by extracting Notifications.
- The boundaries exist only as long as they are enforced. Without the architecture tests, a monolith's modules drift into a big ball of mud; with them, a violation fails the build.
- All modules share one runtime: a memory leak or crash in one affects all. That is acceptable at this scale and is one of the reasons article 11 discusses when to split.
- **What would justify splitting a module out:** independent scaling needs, a different release cadence, a separate team owning it, or a failure that must not take the rest down (notifications must not block bookings).
