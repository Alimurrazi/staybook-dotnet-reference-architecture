# ADR 3: Wolverine instead of MediatR; no AutoMapper

- **Status:** Accepted
- **Date:** 2026-10-07
- **Article:** 1

## Context

The application layer (article 3) needs commands, queries and a pipeline for validation, logging and transactions. Later articles need durable messaging with an outbox (article 7), sagas with scheduled messages (article 8) and a broker transport (article 11).

In .NET, the usual choices are MediatR for in-process commands, MassTransit or NServiceBus for messaging and sagas, and AutoMapper for object mapping. The licensing of several of these changed in 2025 (checked 2026-10-05, see `docs/planning/verification/article-01.md`):

| Library | License |
|---|---|
| MediatR v13+ | Dual-licensed: RPL-1.5 or a Lucky Penny commercial license (free Community license under $5M revenue) |
| AutoMapper 15.0.1+ | Same dual license and Community terms |
| MassTransit v9 | Commercial; v8 stays open source |
| Wolverine | MIT |

## Decision

Use **Wolverine** for both in-process handling (commands, queries, middleware) and messaging (outbox, sagas, scheduled messages, transports). Map objects **by hand**; no mapping library.

In article 1, Wolverine is **registered only**: no handlers, no durable storage. Handlers arrive in article 3 and the outbox in article 7.

Wolverine 6 ships its runtime code compiler as a separate package, `WolverineFx.RuntimeCompilation`. Without it the host fails at startup in the default dynamic code generation mode, even with no handlers. It is part of Wolverine, not an additional library.

## Alternatives considered

| Alternative | Why not |
|---|---|
| MediatR for commands + MassTransit for messaging | Two libraries for one job, two pipelines to configure, and both now commercial or reciprocal-licensed in their current versions |
| MediatR v12 (last Apache-licensed version) | Frozen; a reference architecture shouldn't start on an unmaintained major version |
| Hand-written mediator | Possible in ~50 lines, but the outbox, sagas and transports are not; we would still need a messaging library |
| AutoMapper or Mapster | Mapping by hand is explicit, refactor-safe and trivially debuggable. The DTOs in this codebase are small; the convenience isn't worth a hidden layer of runtime configuration |

## Consequences

- One library, one handler model and one middleware pipeline from article 3 to article 11.
- Wolverine handlers are plain methods discovered by convention. That is less ceremony than MediatR's `IRequestHandler<,>`, but the conventions need explaining (article 3).
- Wolverine generates handler code at runtime. Startup is slower in development, and the generated code needs the runtime compiler package (above). Pre-generating the code (`codegen write`) is a deployment concern for the deployment series.
- Licenses are recorded here and must be rechecked before publishing; the plan's risk table tracks it.
- Mapping code is written and tested by hand. That costs a few lines per endpoint, and nothing maps a field silently.
