# Architecture Decision Records

Each significant decision gets a short record: the context, the decision, the alternatives considered and the consequences. They are numbered in the order they are written, which is the order of the articles (plan section 7 lists them all).

An ADR is never edited to change its decision. If a decision changes, a new ADR supersedes it and the old one's status says so.

| # | Decision | Article | Status |
|---|---|---|---|
| [1](0001-modular-monolith.md) | Modular monolith over microservices | 1 | Accepted |
| [2](0002-solution-structure.md) | One project per module plus a Contracts project | 1 | Accepted |
| [3](0003-wolverine-no-mediatr-no-automapper.md) | Wolverine instead of MediatR; no AutoMapper | 1 | Accepted |
| [4](0004-marten-on-postgresql.md) | Marten on PostgreSQL for documents and events | 1 | Accepted |
