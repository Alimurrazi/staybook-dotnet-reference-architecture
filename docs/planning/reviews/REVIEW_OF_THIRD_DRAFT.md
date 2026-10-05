I'd combine the feedback into one revision strategy for Staybook and one companion payment series. The goal is to preserve your 14-article plan while addressing the remaining correctness issues and giving payments the depth they deserve.

## 1. Keep Staybook focused on application architecture

Your revised plan already has a strong progression: domain modeling → CQRS → event sourcing → availability → reliable messaging → sagas → payments → security and verification. I wouldn't restructure it again.

I would make these targeted changes:

| Area                                | Combined recommendation                                                                                                                           |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| Deliberately broken implementations | Demonstrate failures through isolated examples and tests, rather than leaving unsafe booking or payment endpoints enabled in the main application |
| Booking and Availability            | Use stable operation IDs and idempotent allocation commands; test crashes and lost responses between allocation and booking persistence           |
| Payment outcomes                    | Model succeeded, failed and pending/unknown explicitly; a timeout is not proof of failure                                                         |
| Payment recovery                    | Before retrying or compensating, determine whether the original provider operation succeeded                                                      |
| Financial records                   | Persist payment state and ledger postings atomically, while acknowledging that Stripe calls remain outside the database transaction               |
| Reconciliation                      | Account for legitimate differences such as provider fees and settlement delays                                                                    |
| Article scope                       | Keep Articles 4, 10 and 11 tightly focused; move advanced payment material into the companion series                                              |

## 2. Adjust the payment-related Staybook articles

ARTICLE 5

Introduce the payment boundary

Define `IPaymentGateway`, durable payment operation IDs and a configurable fake gateway. Demonstrate authorization and capture without requiring Stripe. Use tests to expose uncertain outcomes.

ARTICLE 8

Build reliable booking sagas

Implement request-to-book and instant-book workflows. Cover hold conversion, authorization, capture, expiry and compensation. Crucially, demonstrate recovery when a payment may have succeeded but its response was lost.

ARTICLE 9

Integrate Stripe safely

Introduce Stripe test mode, PaymentIntents, idempotency keys, verified webhooks, durable webhook processing and payment-state synchronization. Keep the fake gateway as the default.

ARTICLE 10

Introduce financial correctness

Implement the minimum viable double-entry ledger, cancellation refunds, simulated payouts and basic reconciliation. Explain the accounting model without trying to cover every financial edge case.

This keeps the main series complete and independently runnable. The payment series can then investigate the same architecture at a much deeper level.

## 3. Create a companion series: Engineering Reliable Payments with .NET

I recommend ten articles in four phases, using Staybook as the running example.

| Phase                       | Article                       | Central question                                                             |
| --------------------------- | ----------------------------- | ---------------------------------------------------------------------------- |
| Foundations                 | 1. Payment lifecycles         | What actually happens between authorization, capture, refund and settlement? |
|                             | 2. Payment module design      | How should payment operations, state and idempotency be modeled?             |
|                             | 3. Stripe integration         | How do we isolate a provider behind a reliable interface?                    |
| Reliability                 | 4. Unknown outcomes           | What happens when Stripe succeeds but our application times out?             |
|                             | 5. Webhooks                   | How do we handle duplicate, delayed, missing and out-of-order events?        |
|                             | 6. Payment sagas              | How do we recover when booking and payment disagree?                         |
| Financial correctness       | 7. Double-entry ledger        | How do we account for every cent without mutable balances?                   |
|                             | 8. Refunds and reconciliation | How do we detect and resolve discrepancies with the provider?                |
| Verification and operations | 9. Failure testing            | Can we prove the system recovers after crashes and concurrent requests?      |
|                             | 10. Security and operations   | How do we secure, observe, audit and recover payment workflows?              |

The companion series should reuse Staybook's payment contracts and booking scenarios, rather than build an unrelated second application. Its implementation can live in a separate branch or repository, with clearly documented integration points.

## 4. Build the failure simulator first

Before implementing the companion series, extend the fake gateway so it can reproduce the following scenarios:

- Capture succeeds, but the HTTP response is lost.
- Authorization is pending when the host accepts.
- A webhook arrives twice, late or out of order.
- A refund succeeds, but the application crashes before recording it.
- The payment provider becomes unavailable during recovery.
- A compensation operation repeatedly fails and requires manual intervention.

Each scenario should become an executable test. This is likely to be the most distinctive feature of the payment series: readers won't just see the happy path; they'll be able to reproduce and investigate the failures.

## 5. Recommended publishing order

1. Build Staybook Phase 1, then prototype the booking and availability transaction boundary before publishing.
2. Complete Staybook Phase 2, including reliable messaging and sagas. This establishes the foundation the payment series will reuse.
3. Finish the basic Stripe and ledger implementation in Staybook. Keep these articles focused on what the rental application needs.
4. Publish the companion payment series, progressively replacing simplified examples with deeper failure handling, accounting and recovery.

I'd retain your weekly cadence and buffer weeks, but treat implementation and passing tests as release criteria rather than fixed publication deadlines.

The combined direction is: Staybook teaches when and why to introduce architectural patterns; the payment series teaches how to make one particularly difficult part of that architecture reliable.




Yes. I would keep Staybook Phase 1 as four articles, with the same central goal: a host can create, price and publish a listing, and a guest can search for it and obtain a quote. That is a strong first milestone because readers finish with a working feature rather than just an architectural skeleton.

I would make a few refinements, mainly to avoid introducing complexity before the domain needs it.

## My preferred Phase 1 revisions

| Article                                        | Recommendation                                                                                                  |
| ---------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| 1. Architecture decisions and domain discovery | Keep the scope, but use one concrete booking scenario to identify bounded contexts and consistency requirements |
| 2. Modern .NET solution setup                  | Keep infrastructure minimal; add only what Articles 3 and 4 actually use                                        |
| 3. Domain modeling with DDD                    | Give pricing and monetary correctness more attention; introduce immutable quote snapshots                       |
| 4. CQRS and application layer                  | Prioritize the complete vertical slice; treat caching as optional rather than essential to the milestone        |

### 1. Article 1: Make domain discovery more concrete

Rather than introducing every module equally, walk through one scenario: a guest requests a stay, the host accepts, and payment is captured. Use that scenario to discover the boundaries between Listings, Pricing, Booking, Availability and Payments.

Then distinguish decisions needed now from decisions that will be validated later. For example, choosing a modular monolith is a Phase 1 decision; the precise Booking–Availability transaction boundary is something you can prototype before Phase 2.

This will prevent Article 1 from becoming a long introduction to technologies that readers won't use for several weeks.

### 2. Article 2: Avoid setting up infrastructure prematurely

The draft introduces Aspire, PostgreSQL, Keycloak, Marten, Wolverine, telemetry, CI, architecture tests and Claude Code in one article.&#x20;

SERIES-PLAN.md

&#x20;That's substantial setup for readers who haven't written any application code yet.

I'd configure PostgreSQL and the essential .NET tooling first, establish module boundaries with architecture tests, and add only basic observability. Keycloak can be registered here but fully configured in Article 4, when authentication becomes necessary. Likewise, register Wolverine without introducing durable messaging until Article 7.

The practical goal should be that readers can clone the repository, start its dependencies, run the tests and understand where the first feature belongs.

### 3. Article 3: Introduce quote snapshots and pricing versions

Your current domain model already includes `Money`, `QuoteCalculator`, currency-safe arithmetic and property-based tests.&#x20;

SERIES-PLAN.md

&#x20;I would add two concepts that will become particularly useful when the separate payment series begins.

Pricing version: Every pricing-plan change increments a version, so the application can identify which rules produced a quote.

Quote snapshot: A quote contains its calculated line items, currency, pricing version and expiry. When a booking is eventually created, its price snapshot is immutable; a host changing the nightly rate cannot silently change an existing booking's price.

You don't need to implement bookings yet. Just define the concepts and test their invariants. Article 5 can demonstrate how a booking consumes a valid quote.

### 4. Article 4: Protect the vertical slice from scope creep

Article 4 currently covers CQRS, middleware, Marten, identity, authorization, API conventions, cross-module communication, projections and caching.&#x20;

SERIES-PLAN.md

&#x20;This is the one article I'd simplify most.

Keep everything necessary for create → price → publish → search → quote. Introduce minimal authentication and ownership from the first endpoint, as planned. Demonstrate the search read model and openly explain its temporary in-process delivery weakness.

Move HybridCache into a short optional section or a repository exercise. Caching doesn't establish the architectural foundation, and introducing it alongside eventual consistency adds another source of potentially stale data.

I'd also include a small test for the cross-module publishing rule: if a pricing plan disappears or changes between validation and publication, document the accepted behavior. The draft explicitly accepts a small race here; readers should understand that this is a conscious consistency trade-off, not an accidental omission.

My final preference: make only these four refinements. Don't introduce the ledger, payment gateway or payment sagas into Phase 1. The pricing model should be designed to support them, but Phase 1 should remain a focused, fully working Listings and Pricing vertical slice.
