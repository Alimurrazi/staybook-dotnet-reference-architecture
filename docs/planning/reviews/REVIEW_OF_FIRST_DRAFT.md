I've reviewed your Staybook: .NET Article Series Plan. My main conclusion is that the series is well structured, but eight articles are too few for the amount of material you're planning to teach. I would expand it to approximately 12–14 articles, without expanding the actual product very much.

Your strongest decision is using one Airbnb-style backend to demonstrate why different architectural patterns exist. It gives readers a continuous story instead of disconnected examples of CQRS, event sourcing, sagas and payments.

The biggest risk is that the series could become a demonstration of how many technologies can fit into a .NET application rather than a guide to making sound architectural decisions. Your plan already recognizes that risk, but several weeks still contain enough material for two or three substantial articles.

## 1. My assessment of the current plan

| Domain and project choice | Excellent. Availability, bookings and payments create genuine architectural problems.                           |
| ------------------------- | --------------------------------------------------------------------------------------------------------------- |
| Technical depth           | Ambitious, with appropriate attention to testing, failure handling and trade-offs.                              |
| Article structure         | Consistent, but several articles are overloaded.                                                                |
| Learning progression      | Good foundation, but some features are introduced after earlier articles depend on them.                        |
| Project scope             | Slightly excessive for an educational reference, particularly reviews and payment event sourcing.               |
| Missing material          | Explicit consistency boundaries, complete failure scenarios, threat modeling and a clearer deployment boundary. |

I would retain the modular monolith, PostgreSQL, Marten, Wolverine and the backend-only approach. I would also retain the decision to integrate testing into every article instead of publishing one enormous testing tutorial at the end.

However, I'd make one important change to the series' positioning: instead of promising a production-grade backend, promise a production-oriented reference architecture. A fully production-grade rental platform would require much more than this series intentionally covers, including operational recovery, regulatory requirements and real payment settlement.

## 2. Which articles need to be split?

| Current article        | Verdict | Reason                                                                                                   |
| ---------------------- | ------- | -------------------------------------------------------------------------------------------------------- |
| 1. Foundations         | Split   | Domain discovery, architecture, Aspire, observability and Claude Code are too much for one introduction. |
| 2. Domain modeling     | Keep    | It has a coherent narrative around listings, pricing and value objects.                                  |
| 3. CQRS                | Keep    | Dense but manageable if you use one end-to-end feature.                                                  |
| 4. Event sourcing      | Split   | Event sourcing, concurrency, projections, versioning and personal-data handling are distinct lessons.    |
| 5. Messaging and sagas | Split   | Reliable messaging deserves an explanation before readers encounter distributed workflows.               |
| 6. Reliability         | Keep    | A focused lesson on eventual consistency, recovery and service extraction.                               |
| 7. Payments            | Split   | Payment integration and financial accounting are each substantial subjects.                              |
| 8. API and security    | Split   | API design and resource-level security both deserve practical, independently testable examples.          |

Your planned 20–30-minute reading time is a useful constraint. An article that introduces five major patterns, three libraries and four kinds of tests will probably need to sacrifice either explanations or code examples to meet it.

## 3. My proposed series: 14 articles

I would reorganize the material into four phases. Each phase should end with a working milestone that readers can run and test.

## Phase 1 — Build the foundation

Readers understand the product and can build a complete vertical slice.

1. Architecture decisions and domain discovery

   Introduce Staybook, its bounded contexts, the modular monolith, Clean Architecture and ADRs. Explain why you aren't starting with microservices.
2. Setting up a modern .NET solution

   Build the .NET 10 solution with Aspire, PostgreSQL, Wolverine, CI, architecture tests and observability. Introduce the Claude Code harness as an optional workflow.
3. Domain modeling with DDD

   Implement listings, pricing, value objects, aggregate invariants and domain events. Use test-driven development throughout.
4. CQRS and the application layer

   Add commands, queries, handlers, validation, Marten persistence, projections and caching. Finish with a complete create → publish → search → quote journey.

## Phase 2 — Solve the booking problem

Move from ordinary CRUD to a system where concurrent operations and historical state matter.

5. Event sourcing with Marten

   Model the booking aggregate, event streams, state reconstruction, optimistic concurrency and projections. Introduce event versioning through a concrete change.
6. Availability and concurrency

   Build holds, calendar blocks and booking conflict prevention. Demonstrate the database exclusion constraint and prove correctness with concurrent requests.
7. Reliable messaging with Wolverine

   Explain domain events versus integration events, transactional outbox/inbox, delivery guarantees, idempotency and retries.
8. Sagas and long-running workflows

   Implement request-to-book, expiration and cancellation. Introduce compensation, controlled time and failure recovery.

## Phase 3 — Handle money and distributed failures

Show how external systems change the application's consistency and reliability requirements.

9. Payments and external integrations

   Implement the payment gateway abstraction, fake gateway, Stripe test integration, webhooks and idempotency. Connect payment outcomes to the booking lifecycle.
10. Financial correctness and reconciliation

    Build the ledger, calculate platform fees, simulate host payouts, handle refunds and reconcile recorded payments against the provider.
11. Distributed reliability and observability

    Add dead-letter handling, replay, projection-lag monitoring and distributed tracing. Extract Notifications to demonstrate a real service boundary.

## Phase 4 — Secure and evaluate the system

Finish with a usable, secured API and evidence that the design works.

12. Production-oriented API design

    Cover HTTP semantics, ProblemDetails, API versioning, pagination, concurrency headers, idempotency keys, rate limiting and OpenAPI.
13. Authentication, authorization and API security

    Implement Keycloak, role- and resource-based permissions, webhook verification, threat modeling and access-matrix tests.
14. End-to-end verification and architecture retrospective

    Run complete booking journeys, concurrency and fault-injection tests. Evaluate which architectural choices were justified, which were expensive, and what should be simplified for a smaller application.

This is a larger publishing commitment, but the amount of implementation work is not dramatically greater. Most of the additional articles come from dividing work you already intend to do.

There is also a natural option to publish each phase as its own mini-series, with a working GitHub release at the end.

## 4. The most important architectural problems to resolve

These are more consequential than the article count because they affect the correctness of the final implementation.

### A. Payments appear too late

Your original week 4 introduces instant booking as an authorized-and-captured transaction. Week 5 then introduces the request-to-book saga, including authorization, capture and refunds. But the Payments module is not implemented until week 7.

That creates a dependency problem.

I recommend introducing `IPaymentGateway` and a minimal fake implementation before the first booking workflow. Initially, the fake only needs to support authorization, capture and voiding. When you reach the payments article, implement the Stripe adapter and expand the financial model.

This keeps every earlier milestone executable without prematurely teaching all of Stripe.

### B. Event sourcing does not automatically prevent double booking

Your design uses event-sourced bookings, a per-listing availability aggregate and a PostgreSQL exclusion constraint. These are sensible choices, but their respective responsibilities need to be explicit.

The key design question: What is the authoritative source of truth for whether a date is available?

The booking event stream records the reservation's history. The availability model controls whether a date can be allocated. The database constraint must protect the authoritative availability records, not merely an asynchronously updated projection.

If two guests attempt to book the same dates simultaneously, both might pass an application-level availability check. Your database transaction must ensure that only one obtains the reservation.

I would make this the central problem of the dedicated concurrency article. It is one of the most valuable real-world lessons in the entire series.

### C. Simplify event sourcing in Payments

You propose event sourcing for both Booking and Payments. I would retain event sourcing for Booking but make it an explicit architectural experiment for Payments rather than a predetermined choice.

An immutable double-entry ledger already gives you a permanent financial record. A payment state machine can use conventional persistence, while gateway interactions and financial postings are recorded separately.

Adding an event-sourced Payment aggregate creates another historical model, event-versioning requirements and more projections. It may be justified, but you should first demonstrate what it adds beyond a conventional payment record plus an immutable ledger.

For an educational series, comparing those alternatives may be more valuable than implementing the more complex option automatically.

### D. Reconsider the timing of security

Authentication and authorization cannot realistically wait until the final phase if earlier articles expose endpoints for hosts, guests and payments.

Introduce a minimal identity and ownership model alongside the first endpoints. Expand it into a dedicated security article later, when you have enough resources and workflows to demonstrate real attacks and access-control mistakes.

Likewise, introduce stable API conventions early, rather than changing all existing endpoints during the final API-design article.

## 5. What I would remove, defer or add

## Reduce or defer

- Double-blind reviews: This introduces another timed saga but teaches relatively little beyond request expiration. Make it a bonus exercise.
- Event-sourced Payments: Make the choice conditional on the trade-off analysis above.
- Crypto-shredding: Important, but an advanced topic deserving a separate article rather than a brief section within event sourcing.
- Excessive testing libraries: You have roughly a dozen testing tools listed. Choose one per purpose and introduce alternatives only where the comparison adds value.
- Claude Code in every article: Keep the optional workflow, but don't force an AI section where there is no interesting architectural or development lesson.

## Add or strengthen

- Consistency boundaries: Identify which operations require immediate consistency and which can tolerate eventual consistency.
- Failure-state modeling: Explicitly cover what happens when a payment succeeds but the booking confirmation fails, when a webhook is delayed, and when compensation fails.
- Projection recovery: Include rebuilding read models and handling changes to event schemas.
- Threat modeling: Demonstrate attacks against booking ownership, payment endpoints, webhook processing and resource authorization.
- Operational limitations: Explain what the educational project does not guarantee without production deployment, backup, recovery and security validation.

One additional adjustment: your plan places the Notifications service extraction before the full payment implementation. That is reasonable because Notifications is a low-risk service to extract, but explain that this is a teaching choice. It doesn't imply Notifications would necessarily be the first service extracted in a real rental platform.

## 6. How I would manage the publishing schedule

I agree with your idea of building ahead before publishing, but I would organize the work around complete phases rather than a fixed number of articles.

## Recommended release strategy

| Before publishing  | Finish Phase 1 and validate the core booking and availability design.                                              |
| ------------------ | ------------------------------------------------------------------------------------------------------------------ |
| Publishing cadence | One article per week, provided the corresponding implementation and tests are complete.                            |
| GitHub releases    | Tag every article and create a milestone release at the end of each phase.                                         |
| Reader experience  | Provide a short README, prerequisites, seed data, executable HTTP requests and expected results for every article. |

I would also keep a single architecture diagram that evolves across the series. In the first article, it shows the modular monolith. Later articles progressively add the event store, outbox, broker, external payment provider and extracted Notifications service. This makes it easier for readers to understand why the system grows more complex.

My final recommendation: Adopt the 14-article structure, but resist adding more product features. The central value of Staybook should be demonstrating how a simple rental application evolves when it encounters real problems: concurrency, consistency, external failures, financial correctness and security.

Your most distinctive articles are likely to be the ones on double-booking prevention, reliable messaging and payment failure recovery. Give those enough space to include actual failing scenarios, tests and alternative designs. Those are the articles that can distinguish this series from an ordinary Clean Architecture tutorial.