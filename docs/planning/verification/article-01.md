# Verification before article 1

*Checked 2026-10-05 · Covers the "before article 1" items in plan section 16.*

| # | Item | Result |
|---|---|---|
| 1 | xUnit v3 support in FsCheck, Alba, Stryker.NET | OK, with one condition (Stryker.NET must use its MTP runner) |
| 2 | ArchUnitNET maintenance and .NET 10 support | OK, but a known bug must be worked around |
| 3 | Marten and Wolverine licensing | Confirmed MIT |
| 4 | MediatR, AutoMapper, MassTransit v9, FluentAssertions v8 | Commercial or dual-licensed; the plan's choices avoid all four |
| 5 | Airbnb fees and cancellation policies | Both have changed; the plan's rules now differ from Airbnb's current ones |

## 1. Test tooling and xUnit v3

- **xUnit v3:** latest stable is 4.0.1 (2026-09-12). It installs the Microsoft Testing Platform (MTP) v2 integration by default.
- **FsCheck:** `FsCheck.Xunit.v3` 3.4.0 (2026-08-20) depends on `xunit.v3.extensibility.core` >= 4.0.0 < 5.0.0, so it matches xUnit v3 4.x.
- **Alba:** 8.5.3 (2026-07-11) targets net8.0/9.0/10.0 and isn't tied to any test framework.
- **Stryker.NET:** works with xUnit v3 **only through the MTP runner** (`test-runner: mtp`). The runner arrived as a preview in 4.13 (March 2026). 5.0.0 (2026-09-11) targets .NET 10 and adds per-test coverage analysis to it. The older VSTest path still breaks with xUnit v3 (issue #3117 is open: unexpected test cases and badly wrong mutation scores).
  - **Action:** configure `test-runner: mtp` when Stryker.NET is added (article 14), and check the mutation score against a known mutant before trusting it.

## 2. ArchUnitNET

- `TngTech.ArchUnitNET` 0.13.4 and `TngTech.ArchUnitNET.xUnitV3` 0.13.4 (both 2026-08-20). Apache 2.0, actively maintained. The library targets netstandard2.0, so it runs on .NET 10.
- **Known bug (issue #498, open, 2026-09-24):** in **Release** builds, dependencies inside `async` methods are lost, so negative rules such as `NotDependOnAny` **pass silently**. Debug builds aren't affected. Our handlers and endpoints are async, so in Release this would quietly turn off the module-boundary rules.
- **Action for article 1:**
  - Run architecture tests against Debug builds in CI.
  - Add a guard test that fails if the analyzed assemblies are JIT-optimized (`DebuggableAttribute.IsJITOptimizerDisabled`).
  - Add a canary test: a deliberately forbidden dependency inside an async method must be detected.
  - This also gives article 1 a real "tests that pass for the wrong reason" lesson.

## 3. Marten and Wolverine

- Both `LICENSE` files are MIT (Marten: Jeremy D. Miller, Babu Annamalai, Oskar Dudycz, Joona-Pekka Kokko and contributors; Wolverine: Jeremy D. Miller and contributors).
- JasperFx's model is "open core". The commercial parts are support, consulting, CritterWatch (BSL; read-only monitoring is free) and, according to JasperFx, "some advanced features for data privacy, multi-tenancy and extreme scalability".
- No feature in articles 1 to 14 depends on those add-ons.
- **Watch item for bonus B2** (personal data in an immutable store): JasperFx lists data privacy among the commercial add-ons. Check before writing B2.

## 4. Other library licenses (for ADR 3 and the risk table)

| Library | Status |
|---|---|
| MediatR | v13+ dual-licensed: RPL-1.5 or Lucky Penny commercial license. Free Community license for under $5M revenue, education and non-production use |
| AutoMapper | 15.0.1+ under the same dual license and Community terms |
| MassTransit | v9 is commercial (about $4,000/year for small and medium businesses); v8 stays open source |
| FluentAssertions | v8 is commercial for commercial use (about $129.95 per developer per year); v7 stays Apache 2.0 |

ADR 3 should describe MediatR and AutoMapper as "dual-licensed (RPL-1.5 or commercial)", not "paid only".

## 5. Airbnb's fees and cancellation policies (2026)

**Fees:**

- Airbnb has moved most hosts to a **single host-only fee of about 15.5%** (16% in Brazil and Mexico). The guest pays no separate service fee.
- The **split fee** (host 3%, guest 14.1% to 16.5%) is being phased out.
- The plan uses the split fee (guest 14%, host 3%).

**Cancellation:**

- The standard short-stay policies are now **Flexible, Moderate, Limited (new on 2025-10-01) and Firm**.
- **Strict** is invitation-only.
- Airbnb's **Moderate** pays the host one night plus 50% of the remaining nights after the 5-day cutoff. The plan's version refunds 50% of all nights.
- There is a universal **24-hour grace period** for bookings made at least 7 days before check-in.

**Decisions needed (see the summary in chat):**

- Keep the split fee, labelled as "modeled on Airbnb's older split-fee model"?
- Keep or update the cancellation table?

## Sources

- https://www.nuget.org/packages/xunit.v3
- https://www.nuget.org/packages/FsCheck.Xunit.v3
- https://www.nuget.org/packages/Alba
- https://stryker-mutator.io/blog/stryker-net-mtp-runner/
- https://github.com/stryker-mutator/stryker-net/releases
- https://github.com/stryker-mutator/stryker-net/issues/3117
- https://www.nuget.org/packages/TngTech.ArchUnitNET.xUnitV3
- https://github.com/TNG/ArchUnitNET/issues/498
- https://github.com/JasperFx/marten/blob/master/LICENSE
- https://github.com/JasperFx/wolverine/blob/main/LICENSE
- https://jeremydmiller.com/2026/08/28/the-open-core-model-for-sustainable-oss-development-in-net/
- https://critterwatch.jasperfx.net/deployment/licensing
- https://luckypennysoftware.com/faq
- https://milanjovanovic.tech/blog/mediatr-and-masstransit-going-commercial-what-this-means-for-you
- https://www.infoq.com/news/2025/01/fluent-assertions-v8-license/
- https://www.airbnb.com/help/article/1857 (service fees)
- https://www.airbnb.com/help/article/475 (cancellation policies)
