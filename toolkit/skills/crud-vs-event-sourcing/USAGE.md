# How to use - CRUD vs Event Sourcing

**What it is.** A decision skill that tells you whether an aggregate should be plain **CRUD**, **CRUD + integration events (outbox)**, or **Event Sourcing (usually with CQRS)** - and then gives you the concrete path in your stack for whichever it picks. It runs a fixed 8-signal scoring framework (domain richness, value of history, audit, temporal queries, explaining state, read/write divergence, behavioral analytics, complexity budget) so the answer is defensible, not a vibe. The default is CRUD; Event Sourcing has to earn it. The scoring is stack-neutral; the implementation sketch matches your stack (.NET, Node/TypeScript, Python, Java/Kotlin, Go).

**When to reach for it.** You're modeling a new feature and someone asked "should this be event-sourced?", a review where an aggregate feels like it wants history, you're tempted by ES because it's fashionable, or you have a CRUD table you keep bolting audit/history columns onto and wonder if it's time to switch.

**How to use it.** Describe the aggregate and what the business needs from it - or point it at an existing entity/model. Example prompts:
- "We have an Order with status transitions and need a full audit trail. CRUD or event sourcing?" (.NET / EF Core)
- "Is event sourcing overkill for a user Profile screen in our NestJS app?"
- "Here's my SQLAlchemy `Account` model - should it become event-driven?"
- "Spring Boot payments service: do I need Axon, or just Spring Modulith events with an outbox?"
- "Go service on Postgres: do I actually need an event store and a message bus, or just an outbox?"

**How to get the best out of it.**
- **Feed it the three C's:** the invariants (behavior), the compliance/audit need, and the concurrency situation. Those three signals move the score the most.
- **Say if it's greenfield or a migration.** For migrations it returns a strangler-fig CRUD -> ES path, not a rewrite.
- **Scope it to one aggregate.** ES is an aggregate-level decision; asking "should my whole app be event-sourced?" gets a deliberate "almost certainly not - here's which parts might be."
- **Mention your database.** On Postgres, a Postgres-backed event store (Marten in .NET, Emmett in Node, or a small hand-rolled event table) is usually the lowest-friction ES path.

**What you get.** Which concept you're actually choosing (persistence vs integration vs CQRS), the score table, one recommendation, an implementation sketch for your stack (from `references/event-sourcing-dotnet.md` or `references/event-sourcing-other-stacks.md`), the honest cost, and the trigger for moving to the next step.

**What it won't do.** It won't reflexively recommend Event Sourcing - most of the time it says CRUD and means it. It won't hide the cost: eventual consistency, event versioning/upcasting, harder ad-hoc queries, and GDPR erasure on an immutable log all get called out before you commit.

**Pairs well with.** `ddd-aggregate-generator` (model the aggregate once ES is chosen), `cqrs-setup` (the read/write split ES implies), and `temporal-tables-setup` (history without full ES when the score lands in CRUD territory).
