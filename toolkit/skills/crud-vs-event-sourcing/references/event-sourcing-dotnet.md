# Event Sourcing in .NET - implementation reference

Concrete patterns for the recommendation the skill lands on. Use only the section that matches the verdict. Do not paste all of this back to the user - pull the minimal shape they need.

For Node/TypeScript, Python, Java/Kotlin, and Go, see `event-sourcing-other-stacks.md`. Sections 5-7 below (versioning, GDPR, migration) are stack-neutral concepts.

## 0. Which tool for which verdict

| Verdict | What to reach for |
| --- | --- |
| CRUD | EF Core. Nothing else needed. |
| CRUD + integration events | EF Core + a **transactional outbox** (MassTransit outbox, or a hand-rolled `OutboxMessages` table drained by a background worker) publishing to RabbitMQ/Azure Service Bus/Kafka. |
| ES for one aggregate | **Marten** (Postgres) - lowest-friction ES in .NET; event store + async projections in one library. |
| ES + CQRS at scale | **Marten** (Postgres) or **EventStoreDB/Kurrent** (purpose-built event store with subscriptions) + separate read store (Postgres/Elastic/Redis) fed by projections. |

Rule of thumb: if the team is already on Postgres, Marten is the fastest correct path. Reach for EventStoreDB/Kurrent when you need first-class subscriptions, huge stream volumes, or an event store as a shared backbone across services.

## 1. CRUD (the default) - don't over-build

```csharp
public class Product           // just an entity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public DateTime ModifiedAtUtc { get; set; }
    public string ModifiedBy { get; set; } = "";
}
```

`ModifiedAtUtc`/`ModifiedBy` (or EF Core's temporal tables - `builder.ToTable(t => t.IsTemporal())` on SQL Server) cover "who changed it last" and basic history *without* Event Sourcing. Exhaust this before going further.

## 2. CRUD + integration events (transactional outbox)

The single most common real need behind "should this be event-driven?": state stays CRUD, but other systems must react to changes reliably. The trap is the **dual-write problem** - saving to the DB and publishing to the broker are two operations; a crash between them loses or duplicates events. The outbox fixes it by writing the event into the *same DB transaction* as the state change, then a worker publishes it.

```csharp
// same transaction as the business change
await using var tx = await db.Database.BeginTransactionAsync(ct);
order.MarkPaid();
db.Outbox.Add(new OutboxMessage {
    Id = Guid.NewGuid(),
    Type = nameof(OrderPaid),
    Payload = JsonSerializer.Serialize(new OrderPaid(order.Id, order.Total)),
    OccurredUtc = DateTime.UtcNow
});
await db.SaveChangesAsync(ct);
await tx.CommitAsync(ct);
// a background worker polls Outbox -> publishes to the broker -> marks sent
```

MassTransit ships a production outbox (`AddEntityFrameworkOutbox`) so you rarely hand-roll this. Consumers must be **idempotent** (dedupe on message Id) because delivery is at-least-once.

## 3. Event Sourcing for one aggregate (Marten)

The write model is the event stream; state is a fold over events.

```csharp
// events - immutable facts, past tense
public record AccountOpened(Guid Id, string Owner);
public record MoneyDeposited(decimal Amount);
public record MoneyWithdrawn(decimal Amount);

// aggregate rebuilt by applying events (Marten calls Apply by convention)
public class Account
{
    public Guid Id { get; private set; }
    public decimal Balance { get; private set; }

    public void Apply(AccountOpened e) { Id = e.Id; Balance = 0; }
    public void Apply(MoneyDeposited e) => Balance += e.Amount;
    public void Apply(MoneyWithdrawn e) => Balance -= e.Amount;
}

// command handling: enforce invariants, then append events (never mutate state directly)
public static class Deposit
{
    public static IEnumerable<object> Handle(Account acc, decimal amount)
    {
        if (amount <= 0) throw new InvalidOperationException("Amount must be positive.");
        yield return new MoneyDeposited(amount);
    }
}
```

```csharp
// append
session.Events.StartStream<Account>(accountId, new AccountOpened(accountId, "Stefan"));
session.Events.Append(accountId, new MoneyDeposited(100m));
await session.SaveChangesAsync();

// load current state (fold)
var account = await session.Events.AggregateStreamAsync<Account>(accountId);

// as-of / temporal reconstruction - the ES superpower
var asOfMarch = await session.Events.AggregateStreamAsync<Account>(
    accountId, timestamp: new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
```

**Optimistic concurrency:** append with the expected version so two concurrent writers can't both succeed:
```csharp
session.Events.Append(accountId, expectedVersion, newEvent);
```
**Snapshots:** for long streams, Marten's inline/live aggregation avoids re-folding thousands of events every load.

## 4. Read side (CQRS projections)

Reads never fold the stream ad hoc - you project events into query-optimized read models.

```csharp
public class AccountSummary  // a read model
{
    public Guid Id { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
}

public class AccountSummaryProjection : SingleStreamProjection<AccountSummary>
{
    public void Apply(MoneyDeposited e, AccountSummary v) { v.Balance += e.Amount; v.TransactionCount++; }
    public void Apply(MoneyWithdrawn e, AccountSummary v) { v.Balance -= e.Amount; v.TransactionCount++; }
}
// register: opts.Projections.Add<AccountSummaryProjection>(ProjectionLifecycle.Async);
```

`Inline` = updated in the same transaction as the append (read-your-writes, simplest). `Async` = a background daemon updates it (scales, but **eventually consistent** - the UI may briefly show stale data; design for it).

## 5. Event versioning / upcasting

Events are immutable and live forever, so schemas must evolve without rewriting history. Prefer additive changes (new nullable fields). For breaking changes, **upcast** old event versions to new on read:

```csharp
public record OrderPlacedV1(Guid Id, decimal Total);
public record OrderPlacedV2(Guid Id, decimal Total, string Currency);
// register an upcaster: V1 -> V2 defaulting Currency = "USD"
```
This is a real, recurring tax of ES. Budget for it.

## 6. GDPR / right-to-erasure in an immutable log

You cannot just delete an event. Standard approach: **crypto-shredding** - store personal data encrypted with a per-subject key; to "erase", delete the key so the ciphertext in the events becomes unrecoverable. Decide this *before* adopting ES if the aggregate holds PII.

## 7. CRUD -> ES migration path (when a CRUD aggregate outgrows itself)

The trigger to migrate: you start bolting audit tables, `History` shadow tables, and status-transition columns onto a CRUD entity to reconstruct "why/when" - that's CRUD asking to become events.

1. **Strangle, don't rewrite.** Introduce events for *new* changes going forward; keep the CRUD table as a projection.
2. **Seed the stream** with a single `MigratedFromLegacy` event carrying the current snapshot, so history starts clean without faking the past.
3. **Dual-run:** append events *and* keep the CRUD row updated (via an inline projection) until reads are fully moved to projections.
4. **Cut reads over** to projections; retire direct CRUD reads.
5. Only event-source this one aggregate - leave the rest of the schema CRUD.

## Sources to cite in a post

- Martin Fowler - Event Sourcing, CQRS.
- Marten docs - event store & projections (Postgres).
- EventStoreDB / Kurrent docs - streams & subscriptions.
- MassTransit - transactional outbox.
- Greg Young - talks on ES trade-offs and versioning.
