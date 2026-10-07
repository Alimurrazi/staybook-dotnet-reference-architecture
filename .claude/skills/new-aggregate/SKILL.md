---
name: new-aggregate
description: Create a Staybook aggregate (such as Listing or PricingPlan) test-first, with its invariants, lifecycle, domain errors and domain events. Use when a domain concept has an identity, a lifecycle and rules that must hold together in one transaction.
argument-hint: <Name> <Module>
---

Create the aggregate `$ARGUMENTS`.

## 1. Check the boundary first

Before writing code, answer these and state the answers:

- Which rules must be true **in the same transaction**? Only those belong inside this aggregate.
- Which rules involve another aggregate or module? Those are enforced by the application layer (query through contracts) or by eventual consistency, never by loading the other aggregate here. See plan section 9, article 2, "Aggregate boundaries".
- Is there an ADR for this boundary (for example ADR 5 for Listing and PricingPlan)? If not, propose one.

## 2. Specify it first

Use the `test-writer` subagent. Give it:

- the lifecycle (for Listing: draft → published → unlisted) and which transitions are allowed;
- every invariant, and the typed domain error returned when it's broken (`ListingNotPublishable`, `MinimumNightsNotMet`);
- the domain event each state change records (`ListingPublished`).

## 3. Implement it

Location: `src/Modules/<Module>/Staybook.<Module>/Domain/<Name>/`.

- A strongly typed ID (use `/new-value-object <Name>Id`).
- A private or internal constructor; a static factory for creation that enforces the invariants.
- Public methods named after domain actions (`Publish()`, `Unlist()`), returning a `Result` for expected failures. **No public setters.**
- State changes record domain events in the aggregate; nothing is published from the domain.
- Domain errors as a static class of typed errors next to the aggregate.
- Time comes in as a parameter or a `TimeProvider`, never from `DateTime.UtcNow`.
- No persistence attributes and no references outside `Domain` and `SharedKernel`. Marten maps it in `Infrastructure`.

## 4. Verify

- The tests from step 2 pass.
- The architecture tests pass (the domain stays pure).
- Ask the `architecture-reviewer` subagent to check for anemic models, public setters and boundary leaks.
