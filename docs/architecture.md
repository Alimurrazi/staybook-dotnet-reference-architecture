# Staybook architecture

This file holds the context map and the architecture diagram. The diagram grows with the series: each article that changes the architecture updates it and notes what was added.

## Context map

Staybook's bounded contexts and how they talk to each other: the target for the whole series. Arrows for modules that don't exist yet are refined by the article that builds them. Every arrow is one of the three allowed forms (plan section 7):

- **query** — synchronous read through the other module's Contracts
- **command** — synchronous call through Contracts, **only** for steps the user waits for, always with a stable operation ID
- **message** — asynchronous, through Wolverine

The plain lines from Identity are not communication: they show that modules depend on Identity's contracts (`ICurrentUser`).

```mermaid
flowchart LR
    Identity["<b>Identity</b><br/>users, roles,<br/>current user"]
    Listings["<b>Listings</b><br/>listing lifecycle,<br/>search read model"]
    Pricing["<b>Pricing</b><br/>pricing plans, versions,<br/>stored quotes"]
    Booking["<b>Booking</b><br/>reservation lifecycle<br/>(event-sourced)"]
    Availability["<b>Availability</b><br/>allocations: holds,<br/>bookings, blocks"]
    Payments["<b>Payments</b><br/>operations, outcomes,<br/>ledger, payouts"]
    Notifications["<b>Notifications</b><br/>email"]

    Listings -- "query: has a pricing plan?" --> Pricing
    Booking -- "query: quote by QuoteId" --> Pricing
    Booking -- "query: listing details" --> Listings
    Booking == "command: allocate nights" ==> Availability
    Booking == "command: authorize (instant book: capture)" ==> Payments
    Booking -. "message: capture, void, refund" .-> Payments
    Payments -. "message: payment outcomes" .-> Booking
    Booking -. "message: BookingConfirmed, …" .-> Notifications
    Listings -- "query: availability view (date search)" --> Availability
    Identity --- |"depends on contracts: ICurrentUser"| Listings
    Identity --- |"depends on contracts: ICurrentUser"| Pricing
    Identity --- |"depends on contracts: ICurrentUser"| Booking
```

| Module | Owns | Schema | Introduced |
|---|---|---|---|
| Listings | Listing lifecycle, house rules, search read model | `listings` | Skeleton in 1; domain in 2; application in 3 |
| Pricing | Pricing plans and versions, fees, stored quotes | `pricing` | Skeleton in 1; domain in 2; application in 3 |
| Identity | Users and roles, the current-user abstraction | `identity` | Skeleton in 1; implemented in 4 |
| Booking | Reservation lifecycle (event-sourced) | `booking` | 5 |
| Payments | Payment operations and outcomes, records, ledger | `payments` | 5, 8 to 10 |
| Availability | Allocations: the only source of truth for dates | `availability` | 6 |
| Notifications | Email to guests and hosts | `notifications` | 7; extracted in 11 |

**Relationship notes:**

- **Listings → Pricing** is a query, not a shared transaction. The race between the check and publication is accepted, tested and documented (article 3).
- **Booking → Availability and Payments** are the only synchronous commands, because the guest is waiting for the answer. For instant book, the capture is synchronous too (decision 23). They carry operation IDs so a retry after a lost response is safe (articles 5, 6, 8).
- **Listings → Availability** serves search by dates from Availability's view. The view may be stale; allocation is the real check. Article 6 settles the exact mechanism.
- **Identity** is upstream of everyone: modules depend on its contracts (`ICurrentUser`), never on Keycloak or raw claims (article 4).

## Architecture diagram

### v1: after article 1

```mermaid
flowchart TB
    Client["API clients<br/>(.http files, tests)"]

    subgraph AppHost["Aspire AppHost (local orchestration)"]
        subgraph Monolith["Staybook.Api: modular monolith"]
            direction LR
            Listings["Listings<br/>(skeleton)"]
            Pricing["Pricing<br/>(skeleton)"]
            Identity["Identity<br/>(skeleton)"]
            Infra["Marten + Wolverine<br/>(registered only)"]
        end
        Postgres[("PostgreSQL 18<br/>database: staybook<br/>schema per module")]
        Keycloak["Keycloak<br/>(registered; configured in article 4)"]
        Dashboard["Aspire dashboard<br/>logs, traces, metrics"]
    end

    Client --> Monolith
    Monolith --> Postgres
    Monolith -. "OpenTelemetry" .-> Dashboard
    Keycloak ~~~ Monolith
```

**Added in article 1:** the modular monolith with three module skeletons, PostgreSQL through Aspire, Marten and Wolverine registered, Keycloak registered but not used, OpenTelemetry to the Aspire dashboard, and architecture tests enforcing the boundaries.
