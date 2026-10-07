# ADR 4: Marten on PostgreSQL for documents and events

- **Status:** Accepted
- **Date:** 2026-10-07
- **Article:** 1

## Context

Staybook stores three kinds of data:

- **Documents**: listings, pricing plans, quotes, payment records (articles 3 to 10).
- **Event streams**: bookings are event-sourced (article 5). Their history matters and their state is rebuilt from events.
- **A relational table with a database constraint**: availability allocations, where PostgreSQL's exclusion constraint prevents double booking (article 6).

The messaging outbox (article 7) must be written in the same transaction as the data it describes.

## Decision

Use **one PostgreSQL database** with **Marten** for documents and event streams.

- Each module's data lives in its own schema (ADR 1).
- Marten integrates with Wolverine, so the outbox shares the database transaction (article 7).
- The allocations table (article 6) is plain SQL with Dapper and DbUp, outside Marten, because the exclusion constraint is the point of that table.
- In article 1, Marten is **registered only**, connected through Aspire's Npgsql data source (which adds the database health check and Npgsql telemetry). Modules register their documents from article 3.

Marten is MIT-licensed (checked 2026-10-05). Its commercial add-ons (monitoring, advanced multi-tenancy, data privacy, scaling) aren't used. Bonus article B2 must recheck the data privacy features before relying on them.

## Alternatives considered

| Alternative | Why not |
|---|---|
| EF Core on PostgreSQL + a separate event store (EventStoreDB/KurrentDB) | Two persistence models and two databases; the outbox can't share a transaction with the event store; more infrastructure for readers to run |
| EF Core only, with a hand-written event store table | Possible, but the series would spend articles building stream versioning, projections and rebuilds that Marten provides and tests |
| A document database (MongoDB, Cosmos DB) | No exclusion constraints and weaker transactional guarantees for the allocation rule; two databases again |
| SQL Server | Fine for documents via EF Core, but Marten needs PostgreSQL, and PostgreSQL's `btree_gist` exclusion constraint is central to article 6 |

## Consequences

- One database to run locally (Aspire starts it) and later to back up and operate.
- Documents, events and the outbox commit atomically.
- Marten stores documents as JSONB. Querying is LINQ over JSON; complex reporting queries are less natural than with relational tables, which is why search uses a dedicated read model.
- Marten generates code at runtime, like Wolverine (ADR 3).
- PostgreSQL becomes a hard dependency of every module. That is the trade-off ADR 1 accepted.
