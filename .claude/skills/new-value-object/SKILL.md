---
name: new-value-object
description: Create a Staybook value object (such as Money, DateRange, StayPeriod or a strongly typed ID) test-first, immutable and always valid. Use when a domain concept is defined by its value rather than an identity.
argument-hint: <Name> [module, or "shared" for SharedKernel]
---

Create the value object `$ARGUMENTS`.

## 1. Decide where it lives

- Used by more than one module and stable (Money, DateRange): `src/Staybook.SharedKernel/`.
- Used by one module: `src/Modules/<Module>/Staybook.<Module>/Domain/`.
- If unsure, put it in the module. Moving it to the shared kernel later is easy; taking it back out is not.

## 2. Specify it first

Use the `test-writer` subagent. Give it:

- the rules the value must always satisfy (for example "an amount never has a currency mismatch", "a range's end is after its start");
- the operations and their results, with the plan's exact examples (`docs/planning/SERIES-PLAN.md`, section 5);
- the properties that hold for any input, for FsCheck (for example "allocation always sums to the original").

## 3. Implement it

- A `readonly record struct` for small values (IDs, amounts), or a `sealed record` when it holds a collection or many fields.
- No public constructor that can produce an invalid value. Creation goes through a factory that validates: `Create(...)` returning a `Result<T>` for input that can be wrong at runtime, or throwing for programmer errors.
- No setters. Operations return a new value.
- Equality comes from the record. Override it only if two representations must compare equal.
- No references outside `Domain` and `SharedKernel`.

## 4. Verify

- `dotnet test --project <test project>`: every test from step 2 passes.
- Ask the `architecture-reviewer` subagent to check the change.
