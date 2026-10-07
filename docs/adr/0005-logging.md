# ADR 5: Logging with Microsoft.Extensions.Logging and OpenTelemetry

- **Status:** Accepted
- **Date:** 2026-10-08
- **Article:** 1

## Context

Every module will log, and later articles depend on those logs: a payment outcome that turns *unknown* must be visible to someone (article 8), one booking must be followed across HTTP, messages and an extracted service (article 11), and nothing in a log may leak a token, a secret or a guest's personal data (article 13).

That needs one logging API in every module, one place the logs go, and logs that can be tied to the traces and metrics of the same request. Staybook already sends traces and metrics through OpenTelemetry to the Aspire dashboard (`Staybook.ServiceDefaults`).

## Decision

- Code logs through **`Microsoft.Extensions.Logging` (`ILogger<T>`)**, the logging API built into .NET.
- Logs leave the process through **OpenTelemetry**, like traces and metrics. Locally they go to the Aspire dashboard, where every log line is linked to its trace. Formatted messages and scopes are included (`IncludeFormattedMessage`, `IncludeScopes`).
- Logs are **structured**: message templates with named placeholders (`"Listing {ListingId} published"`), never string interpolation, so each value is a searchable field.
- **No Serilog** and no other logging framework.
- What to log, at which level and how, is fixed in **`docs/logging-conventions.md` in article 3**, when handlers write the first log lines. Redaction of personal data and secrets is added in **article 13**. Where logs are stored in production, and for how long, belongs to the deployment series.

## Alternatives considered

| Alternative | Why not |
|---|---|
| Serilog | Excellent and widely used, and its sinks were the main reason to choose it. With OpenTelemetry as the export path, the sinks aren't needed. Serilog would add a second logging pipeline next to `ILogger` and its own configuration, and its trace correlation needs extra setup that OpenTelemetry gives for free |
| NLog | The same trade-off as Serilog, with less use in modern ASP.NET Core samples |
| Console logging only | Fine for a single process, but no correlation with traces, no structured search, and nothing to follow across services in article 11 |

## Consequences

- Nothing extra to install: logs, traces and metrics use one pipeline and one dashboard.
- Every log line carries the trace and span ID of the request that wrote it.
- Modules depend only on the `ILogger` abstraction. Handlers and infrastructure may log; **the domain doesn't** (the architecture tests forbid `Microsoft.Extensions.*` in `Domain`). Domain code returns results and records events, and the application layer decides what is worth logging.
- Structured templates need discipline. Article 3 enforces it with source-generated `LoggerMessage` methods and an analyzer rule against string-interpolated log calls.
- Logs aren't an audit trail. Who did what, and when, is recorded deliberately by the audit trail in article 13, not reconstructed from logs that may be sampled, filtered or rotated.
