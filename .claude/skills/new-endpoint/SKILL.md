---
name: new-endpoint
description: Create a Staybook Minimal API endpoint for an existing command or query, following the API conventions, with its authorization policy declared. Use when exposing a use case over HTTP.
argument-hint: <METHOD> <route> <CommandOrQuery> <Module>
---

Create the endpoint `$ARGUMENTS`.

## 1. Follow the conventions

Read `docs/api-conventions.md` (written in article 3). If it doesn't exist yet, stop and say so: endpoints come after the conventions.

- Route under `/v1`, plural nouns, kebab-case (`/v1/listings/{listingId}/quotes`).
- Errors are ProblemDetails (RFC 9457) with the error codes from the conventions.
- Request and response bodies are endpoint-specific records. Never bind a request to a domain object and never return one.
- The client never sends a price, an owner ID or anything the server can determine.

## 2. Declare who may call it

Every endpoint declares `RequireAuthorization(<policy>)` or an explicit `AllowAnonymous()`. A convention test fails the build otherwise (from article 4). Ownership ("only the host of this listing") is checked in the handler or Wolverine middleware, not by trusting a route value. If the policy is unclear, ask instead of guessing.

## 3. Implement it

Location: `src/Modules/<Module>/Staybook.<Module>/Endpoints/`.

- The endpoint translates HTTP to the command, invokes it through Wolverine, and translates the `Result` to an HTTP response. Nothing else.
- Map it in the module's endpoint registration, not in `Staybook.Api`.

## 4. Verify

- An Alba integration test covers the success response and one error response (from article 3).
- From article 4, add the endpoint to the access-matrix test.
- Add a request with its expected result to the module's `.http` file.
- Ask the `architecture-reviewer` subagent to check the change.
