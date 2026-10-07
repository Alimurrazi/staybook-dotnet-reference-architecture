---
name: new-command
description: Create a Staybook application-layer command or query with its Wolverine handler and FluentValidation validator, test-first. Use when exposing a use case such as "create listing" or "publish listing".
argument-hint: <CommandName> <Module>
---

Create the command or query `$ARGUMENTS`. Commands change state; queries never do.

## 1. Specify it first

Use the `test-writer` subagent for:

- **Validator tests**: input that is malformed (empty title, negative capacity) is rejected before the handler runs.
- **Handler tests**: the handler loads the aggregate, calls one domain method, and saves. Cover the success case and each domain error.

Input validation (shape, required fields, ranges) belongs in the validator. Business rules belong in the aggregate. Don't check the same rule in both.

## 2. Implement it

Location: `src/Modules/<Module>/Staybook.<Module>/Application/<Feature>/`, one folder per use case holding the command, validator and handler.

- The command is a `sealed record` of primitives and contract IDs. It never contains domain objects and **never a price**: quotes are referenced by `QuoteId`.
- The validator is an `AbstractValidator<TCommand>`.
- The handler is a plain Wolverine handler method: load, call the aggregate, persist, return a `Result`. If it grows past that, logic is leaking out of the domain.
- Reading another module's data goes through that module's `Contracts` query interface. Calling another module synchronously is allowed only for steps the user waits for, and carries an operation ID (plan section 7).
- Time comes from `TimeProvider`.
- No mapping library: map by hand.

## 3. Verify

- The tests from step 1 pass.
- Ask the `architecture-reviewer` subagent to check that the handler stays thin.
- If the command needs an HTTP endpoint, continue with `/new-endpoint`.
