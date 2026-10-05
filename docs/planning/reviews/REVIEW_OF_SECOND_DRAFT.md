I've read the whole plan (and the earlier review, so I don't repeat it), and I checked its claims about the reference repo against the code.

Verdict

The plan is in good shape. The revision took on almost everything from the first-draft review: the 14-article split, the minimal payment fake in article 5, allocations as the single source of truth, the payments experiment, early identity, and the failure and consistency sections. Its claims about the reference repo are accurate: it targets net10.0, and in UnitOfWork.cs:45-63 the events are published after CommitAsync(), and a failure there calls RollbackAsync() on a transaction that has already committed.

What's left are mostly design gaps that will surface when you write the code. A few of them would make an article contradict an earlier one, so they're worth settling before Phase 1.

Must fix (these contradict each other or have no mechanism yet)

1. Article 4's search read model has no defined ng are stored as Marten documents, not eventstreams. Marten projections, though, are built from events. "Projecting domain events into the search document" therefore
   needs one of these:
   - event-sourced Listings, which contradicts ADR 9, "event sourcing for Booking only"
   - Wolverine handlers that consume domain even

   On top of that, search filters by maxPrice, ang. So the read model depends on cross-moduleevents before the outbox exists (article 7). Either accept in-process events in article 4 and say openly that article 7
   fixes them, or keep search to Listings data i
2. Search by dates appears in article 4, but availability only arrives in article 6. You also need to say how search filters
   out booked dates without breaking "a projecti". The answer is that search may use staleavailability because allocation is the real check, but the plan needs to state it.
3. Article 5's instant book has no availability works, but between articles 5 and 6 doublebooking is possible. That's actually a gift: say explicitly that article 6's failing race test starts from article 5's
   code.
4. Request-to-book is mapped to the wrong article. The features table says articles 5 and 8, but it needs holds (article 6)
   and a saga (article 8). Article 5 only covers
5. Booking calls Payments synchronously, which breaks the module communication rule. Section 7 says state changes across
   modules go through integration events. The sehorize and Capture, which are direct statechanges. Either name payment commands as an explicit exception (an "immediate cross-module step", like allocation), or have
   the saga send messages.
6. Article 11 contradicts article 7 on the extraction. Article 11 says "moving from in-process to RabbitMQ", but article 7
   already adds RabbitMQ with Notifications as icle 7 actually uses (Wolverine local queues?) sothat article 11's extraction is a real change.
7. Hold expiry and the saga timeout aren't coord is swept at hour 23:59 while the host accepts at 23:59:30, the capture succeeds with no allocation. Hold expiry should be the request timeout plus a margin, and accepting
   should fail if converting the hold fails.
8. The availability table has no data-access choice. Marten owns documents and events, but the exclusion-constraint table
   needs Npgsql/Dapper, EF Core, or Weasel, plusto the tech stack and to ADR 12. Also note thatexpiry can't go in the constraint's predicate (now() isn't immutable). Use a partial EXCLUDE ... WHERE (status = 'active')
   and sweep expired rows, which the plan alread

Should fix (design and teaching quality)

- Move event versioning out of article 5. In thes events, readers have no "old" events to upcast. Do it later, when there's real history: for example, when article 8 changes a booking event, or in article 11.
- Articles 5 and 8 are still overloaded. Articlecurrency, instant book, the payment port,projections, rebuilds and versioning. Article 8 covers sagas, timers, cancellation policy, refunds, compensation and failed
  compensation. Moving versioning (above) helps sider moving cancellation to article 10, next torefund postings.
- Instant book also needs compensation. It does n one request. A crash after capture leaves money taken with no booking, and section 6 only covers this for article 9. Either send instant book through the saga too, or list
  this gap in article 5's "What can go wrong".
- The pricing example avoids every hard case. 14% of 330.00 is exactly 46.20, and there's no weekly discount. Add a second
  example that forces rounding (e.g. 3 × 99.99, ount), since Money allocation is a headlinelesson. Also define the weekly discount: is it a percentage, and is it applied before or after the cleaning fee?
- The ledger has no refund example. There's a "G posting uses it. Show a 50% Moderatecancellation: which accounts are reversed, and what happens to the 3% host fee on refunded nights. Also say whether a host
  can cancel after the payout.
- The cancellation cutoffs need a time of day. "≥ 1 day before check-in" needs a check-in time in the listing's time zone
  (e.g. 15:00). Otherwise property tests will ex
- Caching search results on top of async projections means two layers of staleness, and the invalidation is fiddly. Caching
  listing details or pricing plans teaches Hybri
- "Stripe unavailable → no hold is kept": the hold is placed before authorization, so it has to be released explicitly. Say
  so; don't imply it never existed.
- Add API versioning to the article 4 conventions. Decide the scheme there (e.g. a /v1 prefix), or article 12's versioning
  section will rewrite every route.
- Auditing "using events" (article 13) only covers Booking. Listings, Pricing and Payments aren't event-sourced.

Minor or editorial

- ADR numbering: ADR 11 is written in article 2. ADRs are numbered in the order they're created, so renumber it (Wolverine
  becomes ADR 3).
- Heading levels: "Phase 1…4" are ## headings inside numbered section 9, which breaks the document outline. Make them ### and
  the articles ####.
- Testing tools:
  - Respawn is listed but never used. Marten alrync, so drop Respawn or justify it.
  - Article 11's "contract tests" have no tool listed.
  - NetArchTest.Rules hasn't been updated in yeamaintained fork.
  - Pin xUnit v3 vs v2 explicitly and check that FsCheck, Alba and Stryker support it.
- Licensing row: Marten and Wolverine are MIT, bial add-ons. Worth one line, given how muchweight the plan puts on licensing.
- Schedule: one article a week, each with full tE, is tight for articles 6, 9 and 10. Plan abuffer week between phases.
- Admin endpoints in article 11 need the admin r 4 minimal identity seeds an admin user.

Next step

Before building Phase 1, settle points 1, 2 and lk to each other from article 4 onward, andchanging them later means rewriting published code.   