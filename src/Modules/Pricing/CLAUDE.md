# Pricing module

Pricing plans and their versions, fees, discounts and stored quotes.
Schema: `pricing`. Introduced in article 2 (domain) and article 3 (application, quotes).

## Layout

```
Staybook.Pricing/
  Domain/          PricingPlan aggregate, Quote, QuoteCalculator, value objects, errors, events
  Application/     One folder per use case: command or query, validator, handler
  Infrastructure/  Marten configuration
  Endpoints/       Minimal API endpoints, mapped in PricingModule.MapPricingEndpoints
  PricingModule.cs
Staybook.Pricing.Contracts/    What other modules may use: IDs, DTOs, query interfaces, messages
```

## Rules

- **The server always calculates prices.** No command or endpoint accepts a price; clients send a `QuoteId`.
- Every pricing-plan change increments the **pricing version**.
- A quote is **immutable** once created: line items, currency, pricing version, owner and expiry. Changing the plan never changes an existing quote.
- Money is minor units plus currency. Every percentage line is rounded on its own to the minor unit, half away from zero; totals are sums of rounded lines.
- Invariant: guest pays = host payout + platform revenue. Property tests assert it for any input.
- The two pricing examples in plan section 5 are tests with their exact numbers. If a change breaks them, the change is wrong.
- Fee percentages are illustrative and configurable. The model is Airbnb's older split fee (guest fee plus host fee), on purpose.
- Quote expiry uses `TimeProvider`.
