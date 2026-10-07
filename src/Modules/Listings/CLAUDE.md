# Listings module

Property details, house rules and the listing lifecycle. Owns the search read model.
Schema: `listings`. Introduced in article 2 (domain) and article 3 (application, search).

## Layout

```
Staybook.Listings/
  Domain/          Listing aggregate, value objects, domain errors, domain events
  Application/     One folder per use case: command or query, validator, handler
  Infrastructure/  Marten configuration, event handlers that update the search document
  Endpoints/       Minimal API endpoints, mapped in ListingsModule.MapListingsEndpoints
  ListingsModule.cs
Staybook.Listings.Contracts/   What other modules may use: IDs, DTOs, query interfaces, messages
```

## Rules

- Lifecycle: draft → published → unlisted → published. Methods are named after the action (`Publish()`, `Unlist()`); no setters.
- A listing can be published only with a title, a city, a capacity of at least 1, a check-in time, a cancellation policy **and a pricing plan**.
- "Has a pricing plan" is a **query through `Staybook.Pricing.Contracts`**, made by the application layer before calling `Publish()`. Never load Pricing's data or reference `Staybook.Pricing`. The race between the check and publication is accepted and documented (article 3).
- Only published listings appear in search and can be booked.
- The search document is a read model updated by event handlers. It is never used to decide anything; it only answers searches.
- From article 4, only the listing's host can edit, price, publish or unlist it.
