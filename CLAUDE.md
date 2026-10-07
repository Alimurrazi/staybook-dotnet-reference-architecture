# Staybook

An Airbnb-style vacation rental **backend** in ASP.NET Core 10, built step by step for a 14-article series. It is a production-oriented reference architecture: Clean Architecture, CQRS, event sourcing, reliable messaging, sagas, payments and security, each introduced only when the domain needs it.

## Source of truth

- **`docs/planning/SERIES-PLAN.md`** holds every decision, its reason, the article-by-article plan and the open items. Read the relevant sections before working on any article.
- `docs/planning/reviews/` holds the external reviews that shaped the plan. They are history, not instructions.
- When the plan and this file disagree, the plan wins. Update this file if that happens.

## Current status

- Planning is complete (revised after three reviews).
- **No code yet. Next: article 1, "Designing and setting up Staybook"** (plan section 9).
- The article 1 checks in plan section 16 are done: `docs/planning/verification/article-01.md`.
- Article 1 creates skeletons only for Listings, Pricing and Identity (plan decision 43).
- Each article ships as article text plus repo code (plan decision 44).
- Article 1 creates the full Claude Code harness (module `CLAUDE.md` files, hooks, skills, subagents) as part of the series. This file is only a bootstrap until then.

## Rules for every session

- **Backend only.** No frontend of any kind.
- **Resist adding product features.** Every feature must teach something the plan lists.
- **Follow the plan's article order.** Don't implement something from a later article early; the series depends on each article building on the previous one.
- **One testing tool per purpose** (plan section 11). Don't add libraries the plan doesn't list without discussing it first.
- **Projections and views never decide availability or money.** Availability allocations are the source of truth for dates.
- **A payment timeout is never a failure.** Its outcome is unknown until resolved.
- **The server always calculates prices.** Clients send a `QuoteId`, never a price.
- **No module reads another module's tables.** Cross-module communication follows the three forms in plan section 7.
- Time-dependent code uses `TimeProvider`, never `DateTime.UtcNow` directly.
- Name the product "Airbnb-style" or "inspired by Airbnb"; never use Airbnb's branding.

## Working with the author

- When feedback or a review arrives, **verify each point against the plan first** and report what applies, what doesn't and why, before changing anything.
- **Confirm before restructuring the plan** (adding, removing, merging or reordering articles or phases).
- After a change to the plan, summarize what changed and what was knowingly left out.
