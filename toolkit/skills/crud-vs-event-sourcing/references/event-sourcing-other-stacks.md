# Event Sourcing outside .NET - implementation reference

Same verdicts, same concepts as `event-sourcing-dotnet.md` (read its sections 5-7 for versioning, GDPR crypto-shredding, and the CRUD -> ES migration path - they are stack-neutral). This file gives the library choices and minimal code shapes for Node/TypeScript, Python, Java/Kotlin, and Go. Pull only the section for the detected stack and verdict.

## 0. Which tool for which verdict

| Verdict | Node / TS | Python | Java / Kotlin | Go |
| --- | --- | --- | --- | --- |
| CRUD | Prisma / TypeORM / Drizzle | SQLAlchemy / Django ORM | Spring Data JPA | GORM / sqlc / pgx |
| CRUD + integration events | outbox table + worker, or Debezium CDC | outbox table + worker, or Debezium CDC | Spring Modulith event publication registry (+ externalization), or outbox table + Debezium | outbox table + worker, Watermill forwarder |
| ES for one aggregate | Emmett (Postgres event store), or hand-rolled on Postgres | `eventsourcing` library (Postgres persistence) | Axon Framework, or hand-rolled on Postgres | hand-rolled on Postgres (pgx) |
| ES + CQRS at scale | EventStoreDB/KurrentDB client + read store | EventStoreDB/KurrentDB client or `eventsourcing` + read store | Axon Server or EventStoreDB/KurrentDB | EventStoreDB/KurrentDB client + read store |

Rule of thumb: outside .NET and Java there is no single dominant ES library. If the team is on Postgres and ES is scoped to one aggregate, a small hand-rolled event table (section 1) is often the honest, lowest-risk choice. Reach for EventStoreDB/KurrentDB when you need first-class subscriptions, high stream volumes, or a shared event backbone. NestJS projects already have `AggregateRoot` (`apply`, `loadFromHistory`) and an `EventBus` in `@nestjs/cqrs`, but no event store - you still persist events yourself.

## 1. The portable building blocks (any language, Postgres)

**Event store table** - the uniqueness constraint *is* the optimistic concurrency check:
```sql
CREATE TABLE events (
  stream_id       uuid        NOT NULL,
  version         int         NOT NULL,
  type            text        NOT NULL,
  data            jsonb       NOT NULL,
  metadata        jsonb       NOT NULL DEFAULT '{}',
  occurred_at     timestamptz NOT NULL DEFAULT now(),
  global_position bigserial,
  PRIMARY KEY (stream_id, version)
);
```
Append = insert `expectedVersion + 1 ... + n` in one transaction; a unique-violation means another writer won -> return a concurrency conflict. Load = `SELECT ... WHERE stream_id = $1 ORDER BY version`, fold with `evolve`. As-of = add `AND occurred_at <= $2`.

Caveat for async projections that read by `global_position`: sequence values are assigned before commit, so concurrent transactions can commit out of order and a naive "last position" checkpoint can skip events. Project inline (same transaction), or use a library that solves it.

**Outbox table** (CRUD + integration events):
```sql
CREATE TABLE outbox (
  id           uuid        PRIMARY KEY,
  type         text        NOT NULL,
  payload      jsonb       NOT NULL,
  occurred_at  timestamptz NOT NULL DEFAULT now(),
  published_at timestamptz
);
-- worker: SELECT ... WHERE published_at IS NULL ORDER BY occurred_at LIMIT 100 FOR UPDATE SKIP LOCKED
```
Write the outbox row in the *same transaction* as the state change; the worker publishes and marks it sent. Delivery is at-least-once, so consumers must be idempotent (dedupe on message id). Debezium (CDC on the outbox table) replaces the polling worker when the platform already runs Kafka Connect.

## 2. Node / TypeScript

**Decider shape** (works hand-rolled, and maps directly onto Emmett's `decide`/`evolve` model):
```ts
type AccountEvent =
  | { type: "AccountOpened"; id: string; owner: string }
  | { type: "MoneyDeposited"; amount: number }
  | { type: "MoneyWithdrawn"; amount: number };

type Account = { id: string; balance: number };
const initial: Account = { id: "", balance: 0 };

export const evolve = (s: Account, e: AccountEvent): Account => {
  switch (e.type) {
    case "AccountOpened":  return { id: e.id, balance: 0 };
    case "MoneyDeposited": return { ...s, balance: s.balance + e.amount };
    case "MoneyWithdrawn": return { ...s, balance: s.balance - e.amount };
  }
};

// command: enforce invariants, return new events - never mutate state directly
export const withdraw = (s: Account, amount: number): AccountEvent[] => {
  if (amount <= 0) throw new Error("Amount must be positive");
  if (s.balance < amount) throw new Error("Insufficient funds");
  return [{ type: "MoneyWithdrawn", amount }];
};

// handler: load -> fold -> decide -> append with expected version
const { events, version } = await store.readStream<AccountEvent>(accountId);
const state = events.reduce(evolve, initial);
await store.appendToStream(accountId, withdraw(state, 50), { expectedVersion: version });
```
`store` is either Emmett's PostgreSQL event store or a ~50-line wrapper over the table in section 1. Read models: a projection function per read table, run inline or by a worker with a checkpoint.

## 3. Python

**`eventsourcing` library** - aggregates with decorated event methods, an `Application` with a repository, pluggable persistence (SQLite, Postgres):
```python
from eventsourcing.domain import Aggregate, event
from eventsourcing.application import Application

class Account(Aggregate):
    @event("Opened")
    def __init__(self, owner: str):
        self.owner = owner
        self.balance = 0

    def withdraw(self, amount: int) -> None:
        # invariants live in the undecorated command method...
        if amount <= 0 or amount > self.balance:
            raise ValueError("invalid withdrawal")
        self._withdrawn(amount)

    @event("Withdrawn")
    def _withdrawn(self, amount: int) -> None:
        # ...the decorated method is the "apply" and is replayed on load
        self.balance -= amount

class Accounts(Application):
    def withdraw(self, account_id, amount: int) -> None:
        account = self.repository.get(account_id)
        account.withdraw(amount)
        self.save(account)
```
`repository.get(id, version=n)` reconstructs a past version (temporal query). Projections: the library's process/system features, or a plain consumer that reads new events and writes read tables. Without the library, the section 1 table + a dataclass-per-event + `functools.reduce(evolve, events, initial)` is enough for one aggregate.

## 4. Java / Kotlin

**Axon Framework** (aggregate, command handling, event-sourcing handlers, projections, upcasters):
```java
@Aggregate
class Account {
    @AggregateIdentifier private UUID id;
    private BigDecimal balance;

    protected Account() {}

    @CommandHandler
    Account(OpenAccount cmd) { AggregateLifecycle.apply(new AccountOpened(cmd.id(), cmd.owner())); }

    @CommandHandler
    void handle(Withdraw cmd) {
        if (cmd.amount().compareTo(balance) > 0) throw new IllegalStateException("Insufficient funds");
        AggregateLifecycle.apply(new MoneyWithdrawn(cmd.amount()));
    }

    @EventSourcingHandler void on(AccountOpened e)  { id = e.id(); balance = BigDecimal.ZERO; }
    @EventSourcingHandler void on(MoneyWithdrawn e) { balance = balance.subtract(e.amount()); }
}
```
Read models are `@EventHandler` components writing to their own tables, queried via `@QueryHandler`. Axon Framework is open source; check which Axon Server edition (or alternative event store) fits the org before committing. For **CRUD + events** in Spring, Spring Modulith's event publication registry persists events published in the transaction and retries listeners (`@ApplicationModuleListener`), and can externalize them to a broker - an outbox without hand-rolling. Kotlin: same libraries, or the decider shape from section 2 with sealed classes.

## 5. Go

Hand-rolled on pgx is the common path:
```go
type Account struct { ID string; Balance int64; Version int }

func (a *Account) apply(e any) {
    switch ev := e.(type) {
    case AccountOpened:  a.ID, a.Balance = ev.ID, 0
    case MoneyWithdrawn: a.Balance -= ev.Amount
    }
    a.Version++
}

func (a *Account) Withdraw(amount int64) ([]any, error) {
    if amount <= 0 || amount > a.Balance { return nil, ErrInvalidWithdrawal }
    return []any{MoneyWithdrawn{Amount: amount}}, nil
}

func (s *Store) Append(ctx context.Context, streamID string, expected int, evs []Event) error {
    tx, err := s.db.Begin(ctx)
    if err != nil { return err }
    defer tx.Rollback(ctx)
    for i, e := range evs {
        _, err := tx.Exec(ctx,
            `INSERT INTO events (stream_id, version, type, data) VALUES ($1, $2, $3, $4)`,
            streamID, expected+i+1, e.Type, e.Data)
        if isUniqueViolation(err) { return ErrConcurrencyConflict }
        if err != nil { return err }
    }
    return tx.Commit(ctx)
}
```
Outbox: same-transaction insert + a worker, or Watermill's forwarder component over its SQL pub/sub. At scale, use the EventStoreDB/KurrentDB Go client with persistent subscriptions feeding projections.

## 6. Versioning and upcasting per stack

Prefer additive changes everywhere. For breaking changes, upcast old versions on read: a `type -> transform` map applied before `evolve` (Node, Go, hand-rolled Python), the `eventsourcing` library's class-version upcasting, or Axon upcasters (Java). Store a schema version in `metadata` from day one.

## Sources to cite

- Martin Fowler - Event Sourcing, CQRS.
- Oskar Dudycz / event-driven.io - Emmett, decider pattern, outbox and projection pitfalls.
- `eventsourcing` (Python) docs.
- Axon Framework docs; Spring Modulith docs (event publication registry).
- EventStoreDB / Kurrent docs - streams & subscriptions.
- Debezium docs - outbox event router.
- Harry Percival & Bob Gregory - "Architecture Patterns with Python" (message bus, unit of work, events).
