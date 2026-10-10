# Learning .NET Architecture — Part 1: Design and Setup

In this series, we build **Staybook**, a simplified vacation rental backend inspired by Airbnb, using ASP.NET Core 10. Guests find places to stay, get a price, book nights and pay. Hosts publish listings and accept booking requests.

We use this familiar booking flow to explore Clean Architecture, CQRS, event sourcing, reliable messaging, sagas, payments and security—introducing each tool or pattern when we need it.

## The tools

These are the main tools in this article.

- **[PostgreSQL](https://www.postgresql.org/)**: an open-source relational database. Staybook keeps all its data in one PostgreSQL database, with a separate schema for each module.
- **[Marten](https://martendb.io/)**: a .NET library that turns PostgreSQL into a document database and an event store. Staybook uses it to store documents such as listings and quotes, and later the events of each booking.
- **[Wolverine](https://wolverinefx.net/)**: a .NET library for handling commands and messages. Staybook uses it for its handlers, and later for reliable messaging, the outbox and sagas.
- **[Aspire](https://aspire.dev/)**: a tool for running an application with everything it depends on. One command starts PostgreSQL and Keycloak in Docker, connects the API to PostgreSQL, and provides a dashboard. Keycloak remains unused until article 5.
- **[OpenTelemetry](https://opentelemetry.io/)**: an open standard for logs, traces and metrics. Staybook sends them to the Aspire dashboard, so you can see what each request did.
- **[Keycloak](https://www.keycloak.org/)**: an open-source login and identity server. It's started now but used only from article 5, for authentication.
- **[ArchUnitNET](https://github.com/TNG/ArchUnitNET)**: a library for writing tests about the code's structure. Staybook's architecture tests use it to check the module and layer rules.

## What this article builds, and what comes later

**What exists after this article:**

- three module skeletons: Listings, Pricing and Identity;
- PostgreSQL running locally, with Marten and Wolverine registered for later features;
- OpenTelemetry, with logs, traces and metrics in the Aspire dashboard;
- health endpoints;
- 37 architecture tests.

**What comes later:**

- article 2: building with Claude Code;
- article 3: domain modeling;
- article 4: the first business endpoints;
- article 5: authentication with Keycloak;
- article 6: event-sourced bookings;
- article 7: allocations that make double booking impossible;
- article 8: the outbox;
- article 9: sagas.

So after the setup commands you get a running, observable, well-guarded skeleton, not a booking API. That comes over the next few articles.

> **A note on the plan.** The article order, the module boundaries and the architecture in this article are my initial plan. Some of it will probably change as development goes on and the code teaches me something the plan didn't foresee. When that happens, the article that makes the change will say what changed and why, and the decision records in `docs/adr/` will be updated with it.

We keep the scope small: no photos, chat, wishlists or frontend. Each feature helps explain a specific problem or design choice.

Staybook is a **reference architecture built around real-world problems**. It is a learning project, not a finished platform for running a business. It does not handle real payments or include backups, and it has not been reviewed for legal compliance or audited for security.

## Finding the boundaries

Instead of drawing boxes, walk through one scenario: *a guest requests a stay, the host accepts, the payment is captured.*

[![A guest requests a stay, the host accepts, the payment is captured](images/article-01/booking-flow.png)](images/article-01/booking-flow.png)

Watch where the vocabulary changes:

1. The guest finds a **listing**: a property a host offers, with a title, a city, a capacity and house rules. A new listing has the status **Draft**; guests can book it only after the host publishes it. That's **listings**.
2. The guest asks for a price for their dates and number of guests. The server calculates it from the host's **pricing plan** (rates, fees and discounts), rounds every line to whole cents, and saves the result as a **quote**. If the host changes the plan later, an existing quote keeps its price until it expires. Only new quotes use the new prices. That's **pricing**.
3. The guest requests to book with the quote's ID (never a price). The nights go on **hold** (**availability**), the card is **authorized** (**payments**), and the request is recorded (**booking**).
4. The host accepts: the hold becomes a confirmed allocation, then the money is captured.
5. Both get an email (**notifications**). **Identity** tells us who is making the request and which roles they have.

Each place where the words, the rules or the owner change is a likely boundary between two modules. That gives seven modules. They are my starting design: if building a workflow shows that a boundary is wrong, it will change.

Booking and Availability can look like one module, but they do different jobs. **Booking** follows a reservation from the request to the end: waiting for the host, confirmed, cancelled or expired. **Availability** only decides which nights are taken, and makes sure two guests can never get the same night.

Only three modules exist today: Listings, Pricing and Identity. The other four arrive in the articles that need them:

[![Staybook modules and how they talk](images/article-01/context-map.png)](images/article-01/context-map.png)

*Target architecture across the series. Green modules exist after article 1; dashed ones arrive later. Solid arrows are queries, thick arrows synchronous commands, dotted arrows messages.*

## Three ways to talk

When one module needs something from another, it uses one of three forms. If you know HTTP, you can think of them like this:

| Form              | Think of it as                                                                       | Example                                                                  |
| ----------------- | ------------------------------------------------------------------------------------ | ------------------------------------------------------------------------ |
| **Query**   | A`GET`: "give me some data." Nothing changes                                       | Booking reads the guest's quote from Pricing                             |
| **Command** | A`POST`: "do this now," and wait for the answer                                    | Booking asks Availability to hold the nights while the guest waits       |
| **Message** | A request or event processed asynchronously; the caller does not wait for completion | Booking announces`BookingConfirmed`, and Notifications sends the email |

Since everything runs in one application, these aren't real HTTP calls. A query or command is a plain C# method call through the other module's **Contracts** project, its small public part, and a message goes through Wolverine.

Two rules keep this simple:

- Use a synchronous cross-module command only when a user is waiting for the answer. Other cross-module actions use messages.
- Every synchronous cross-module command carries a stable **operation ID**. The receiving module stores the operation and its outcome, so a retry reuses the existing operation rather than repeating the work. An outcome may still be pending or unknown.

## A modular monolith

Seven modules sound like seven microservices. They aren't, yet. Staybook is one application with one PostgreSQL database and hard boundaries inside it:

- modules reference each other only through **Contracts** projects;
- **architecture tests** check project references and type dependencies, and fail the test run when a boundary is crossed;
- every module will own its own PostgreSQL schema, and no module reads another's tables. Today the schema names are declared; persistence isolation arrives as the modules gain storage.

Here's what actually runs after article 1:

[![What runs after article 1](images/article-01/runtime.png)](images/article-01/runtime.png)

*What runs after article 1.*

Microservices would turn every boundary into a network call and a deployment before we know the boundaries are right. When there's a real reason to split, in article 12, the boundaries will already be there.

Each decision has a one-page record in `docs/adr/`: the modular monolith, the project structure, Wolverine instead of MediatR, Marten on PostgreSQL, and logging through `ILogger` with OpenTelemetry.

## The setup, in decisions

The tools are chosen for where the series goes: together, Marten and Wolverine give us documents, events, messaging and sagas on the same PostgreSQL database, so later articles never need a second one.

- **A strict build.** Warnings are errors (except NuGet vulnerability advisories, so a new advisory can't break an old tag), recommended analyzers are on, style rules run in the build, and package versions live in one file.
- **One command to run it.** `dotnet run --project src/Staybook.AppHost` starts PostgreSQL and Keycloak in Docker, wires the connection string and opens a dashboard with logs, traces and metrics.
- **Registered, not configured.** Marten and Wolverine are registered, but nothing uses them until article 4. Infrastructure arrives when it pays for itself.

Running the app, not just building it, mattered: the API compiled fine but crashed at startup. In Wolverine 6's default dynamic code-generation mode, the API needs the separate `WolverineFx.RuntimeCompilation` package to start. A passing build proves the code compiles, not that the application starts.

The solution looks like this:

```
src/
  Staybook.AppHost/          Aspire: PostgreSQL, Keycloak, the API
  Staybook.ServiceDefaults/  OpenTelemetry and health checks
  Staybook.Api/              Composition root, no business logic
  Staybook.SharedKernel/     Shared types, kept small
  Modules/<Module>/
    Staybook.<Module>/            Domain/ Application/ Infrastructure/ Endpoints/
    Staybook.<Module>.Contracts/  The only part other modules may use
tests/Staybook.ArchitectureTests/
```

What each part is for:

- **`Staybook.AppHost`**: the Aspire project you run locally. It starts PostgreSQL and Keycloak in Docker, then starts the API connected to PostgreSQL. Keycloak remains unused until article 5.
- **`Staybook.ServiceDefaults`**: setup every service shares: OpenTelemetry and health checks.
- **`Staybook.Api`**: the application that actually runs. It only wires things together and registers the modules; it holds no business rules itself.
- **`Staybook.SharedKernel`**: a few small types every module needs, such as `Money` and `DateRange` (from article 3). Kept small on purpose, because every module depends on it.
- **`Modules/<Module>/Staybook.<Module>`**: one module's private code, in four folders:
  - `Domain/`: the business rules, such as when a listing can be published or how a price is calculated;
  - `Application/`: the use cases, such as "create a listing", which load data, call the domain and save the result;
  - `Infrastructure/`: database and other technical details;
  - `Endpoints/`: the HTTP API for this module.
- **`Modules/<Module>/Staybook.<Module>.Contracts`**: the module's public part: the interfaces and data other modules may use.
- **`tests/Staybook.ArchitectureTests`**: the tests that check the structure rules described in the next section. Domain unit tests arrive in article 3, followed by database and HTTP integration tests in article 4. Module-specific tests live in `tests/Modules/<Module>.Tests/`.

## Tests that check themselves

Article 1 has no business logic, but it has **37 architecture tests**. They fail the test run when someone breaks one of these rules:

- modules use each other only through their **Contracts** projects;
- the **domain** uses no database, web or logging frameworks;
- dependencies point **inward**: endpoints → application → domain, never the other way;
- the **shared kernel** does not depend on module implementations or the infrastructure frameworks checked by the suite.

## Try it

You need the .NET 10 SDK (10.0.100 or later) and Docker running.

```bash
git clone https://github.com/Alimurrazi/staybook-dotnet-reference-architecture.git
cd staybook-dotnet-reference-architecture
git checkout article-01
dotnet test --solution Staybook.slnx    # Runs in Debug, which the architecture tests require
dotnet run --project src/Staybook.AppHost
```

You should see 37 passing tests. Once the application starts, visit `http://localhost:5174/health`. It should return `Healthy`, confirming that the API can connect to PostgreSQL. This endpoint is available only in Development.

The terminal also prints an Aspire dashboard login URL:

```text
Login URL: http://localhost:15174/login?t=<your-login-token>
```

Open the URL printed in your terminal to view the Aspire dashboard. It displays the local resources, their status, logs, traces and metrics.

<!-- Insert the uploaded Aspire dashboard screenshot here before publishing on dev.to. -->

*Suggested screenshot caption: Aspire dashboard after starting Staybook. Keycloak is provisioned but remains unused until article 5.*

## Next: Building Staybook with Claude Code

Staybook is built with Claude Code. The repository includes the setup that guides its work: `CLAUDE.md` files, permissions, hooks, skills and subagents. We’ll explore how this setup works in the next article.
