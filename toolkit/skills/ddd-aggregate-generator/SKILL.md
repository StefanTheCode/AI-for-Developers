---
name: ddd-aggregate-generator
description: Generate Domain-Driven Design building blocks in any language - aggregate roots, entities, value objects, domain events, and invariant-protecting factory methods - in C#, TypeScript, Python, Java/Kotlin, or Go, mapped cleanly to the project's ORM (EF Core, Prisma/TypeORM, SQLAlchemy/Django, JPA/Hibernate, GORM/sqlc). Use this skill whenever the user wants a DDD aggregate, value object, domain event, rich domain model, bounded context structure, encapsulated invariants, to fix an anemic domain model, or asks "how do I model this domain / build an aggregate root". Always use this skill for DDD modeling requests; it generates a rich, encapsulated model rather than anemic setters.
category: Architecture
version: 1.0.0
---

# DDD Aggregate Generator

Model a domain with proper DDD building blocks: an aggregate root that protects its invariants, value objects for concepts, and domain events - no anemic bags of public setters - in the idiom of the project's language.

## Input - point it at your project
Works on a target: a **file**, **folder**, **project/repo**, or **GitHub URL**. If a domain layer exists, match its base types and conventions (`Entity`, `AggregateRoot`, `ValueObject`, event collection, error/Result type) instead of inventing new ones.

## Step 1 - detect the stack

| Manifest | Stack | Find existing domain code |
|---|---|---|
| `*.csproj` | C# / .NET | `grep -rn "AggregateRoot\|ValueObject\|: Entity\|IDomainEvent" --include=*.cs` |
| `package.json` | TypeScript | `grep -rn "AggregateRoot\|ValueObject\|DomainEvent\|private constructor" --include=*.ts` |
| `pyproject.toml` / `requirements*.txt` | Python | `grep -rn "frozen=True\|class .*Aggregate\|DomainEvent" --include=*.py` |
| `pom.xml` / `build.gradle(.kts)` | Java / Kotlin | `grep -rn "AbstractAggregateRoot\|@Embeddable\|record .*(\|data class" src` |
| `go.mod` | Go | `grep -rn "func New[A-Z]\|type .* struct" --include=*.go internal/domain` |

## Universal building blocks (every stack)
1. **Aggregate root** - the only entry point to a cluster of objects and the **consistency boundary**. Outsiders call intention-revealing methods (`confirm()`, `addLine()`), never set fields.
2. **Factory method** - `create(...)` validates invariants and returns a valid aggregate or an error. No way to construct an invalid one.
3. **Invariants live in the aggregate**, checked in every mutating method. State transitions are explicit (`Pending -> Confirmed`), illegal ones are rejected.
4. **Value objects** for concepts (`Money`, `Email`, `DateRange`, typed IDs): immutable, equality by value, validated on construction.
5. **Reference other aggregates by ID**, not by object navigation. One transaction changes one aggregate.
6. **Domain events** - immutable, past-tense facts (`OrderConfirmed`) raised inside the aggregate, collected, and dispatched after the transaction commits (in-process handlers or an outbox).
7. **Persistence ignorance** - the domain doesn't import the ORM or web framework; mapping lives in infrastructure.
8. **Don't over-DDD.** A lookup table or simple CRUD form stays a plain entity. Say so.

## C# / .NET
```csharp
public sealed class Order : AggregateRoot
{
    private readonly List<OrderLine> _lines = new();
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }

    private Order() { }                              // EF

    public static Result<Order> Create(CustomerId customer, IEnumerable<OrderLine> lines)
    {
        var list = lines.ToList();
        if (list.Count == 0) return Error.Validation("Order needs at least one line");
        var order = new Order { Status = OrderStatus.Pending };
        order._lines.AddRange(list);
        order.Total = Money.Sum(list.Select(l => l.LineTotal));
        order.Raise(new OrderCreated(order.Id));     // domain event
        return order;
    }

    public Result Confirm()
    {
        if (Status != OrderStatus.Pending) return Error.Conflict("Only pending orders can be confirmed");
        Status = OrderStatus.Confirmed;
        Raise(new OrderConfirmed(Id));
        return Result.Success();
    }
}

public sealed record Money(decimal Amount, string Currency)
{
    public static Money Sum(IEnumerable<Money> items) { /* guard same currency */ }
}
```
Records give value equality for free; add guards in the constructor. Map with EF Core: private parameterless constructor, backing field for `_lines`, value objects as owned types or complex properties, strongly typed IDs via value converters. Events are collected on the base type and dispatched after `SaveChanges` (interceptor -> outbox or in-process publisher).

## TypeScript
```ts
export class Money {
  private constructor(readonly amount: number, readonly currency: string) {}
  static of(amount: number, currency: string): Money {
    if (!Number.isFinite(amount) || amount < 0) throw new DomainError("Invalid amount");
    return new Money(amount, currency);
  }
  add(o: Money): Money { if (o.currency !== this.currency) throw new DomainError("Currency mismatch"); return Money.of(this.amount + o.amount, this.currency); }
  equals(o: Money): boolean { return this.amount === o.amount && this.currency === o.currency; }
}

export class Order {
  #status: OrderStatus = "pending";
  #events: DomainEvent[] = [];
  private constructor(readonly id: OrderId, readonly customerId: CustomerId, private readonly lines: OrderLine[]) {}

  static create(id: OrderId, customerId: CustomerId, lines: OrderLine[]): Result<Order, DomainError> {
    if (lines.length === 0) return err(new DomainError("Order needs at least one line"));
    const order = new Order(id, customerId, [...lines]);
    order.#events.push({ type: "OrderCreated", orderId: id });
    return ok(order);
  }

  confirm(): Result<void, DomainError> {
    if (this.#status !== "pending") return err(new DomainError("Only pending orders can be confirmed"));
    this.#status = "confirmed";
    this.#events.push({ type: "OrderConfirmed", orderId: this.id });
    return ok(undefined);
  }

  get status() { return this.#status; }
  pullEvents(): DomainEvent[] { const e = this.#events; this.#events = []; return e; }
}
```
Use branded types for IDs (`type OrderId = string & { readonly __brand: "OrderId" }`). Keep Prisma/TypeORM models in infrastructure and map to/from the domain class in the repository - don't make the ORM entity the aggregate. Use the project's Result type or throw `DomainError` if that's the convention.

## Python
```python
@dataclass(frozen=True)
class Money:
    amount: Decimal
    currency: str
    def __post_init__(self):
        if self.amount < 0: raise DomainError("Invalid amount")
    def __add__(self, other: "Money") -> "Money":
        if other.currency != self.currency: raise DomainError("Currency mismatch")
        return Money(self.amount + other.amount, self.currency)

class Order:
    def __init__(self, id: OrderId, customer_id: CustomerId, lines: list[OrderLine]):
        self.id, self.customer_id = id, customer_id
        self._lines = list(lines)
        self._status = OrderStatus.PENDING
        self.events: list[DomainEvent] = []

    @classmethod
    def create(cls, id: OrderId, customer_id: CustomerId, lines: list[OrderLine]) -> "Order":
        if not lines: raise DomainError("Order needs at least one line")
        order = cls(id, customer_id, lines)
        order.events.append(OrderCreated(order_id=id))
        return order

    def confirm(self) -> None:
        if self._status is not OrderStatus.PENDING:
            raise DomainError("Only pending orders can be confirmed")
        self._status = OrderStatus.CONFIRMED
        self.events.append(OrderConfirmed(order_id=self.id))

    @property
    def lines(self) -> tuple[OrderLine, ...]: return tuple(self._lines)
```
Frozen dataclasses (or Pydantic frozen models) for value objects and events. With SQLAlchemy, imperative mapping keeps the domain class ORM-free; with Django, keep rules in model methods and a custom manager, and accept the trade-off rather than fighting the framework. Collect events and publish them from the unit of work after commit.

## Java / Kotlin
```java
public record Money(BigDecimal amount, Currency currency) {
    public Money { if (amount.signum() < 0) throw new DomainException("Invalid amount"); }
    public Money add(Money o) { requireSameCurrency(o); return new Money(amount.add(o.amount), currency); }
}

@Entity
public class Order extends AbstractAggregateRoot<Order> {
    @Id private UUID id;
    @Enumerated(EnumType.STRING) private OrderStatus status;
    @OneToMany(cascade = CascadeType.ALL, orphanRemoval = true) @JoinColumn(name = "order_id")
    private List<OrderLine> lines = new ArrayList<>();

    protected Order() {}                                   // JPA

    public static Order create(UUID id, UUID customerId, List<OrderLine> lines) {
        if (lines.isEmpty()) throw new DomainException("Order needs at least one line");
        var o = new Order(); o.id = id; o.status = OrderStatus.PENDING; o.lines.addAll(lines);
        o.registerEvent(new OrderCreated(id));
        return o;
    }

    public void confirm() {
        if (status != OrderStatus.PENDING) throw new DomainException("Only pending orders can be confirmed");
        status = OrderStatus.CONFIRMED;
        registerEvent(new OrderConfirmed(id));
    }

    public List<OrderLine> lines() { return List.copyOf(lines); }
}
```
Spring Data publishes events registered via `AbstractAggregateRoot` when the aggregate is saved through its repository; use `@TransactionalEventListener` (or Spring Modulith) to act after commit. No public setters; value objects as records mapped with `@Embeddable` or converters. Kotlin: `data class` / `@JvmInline value class` for value objects, `private set` on properties.

## Go
```go
type Money struct{ Amount int64; Currency string } // comparable: == is value equality

type Order struct {
    id         OrderID
    customerID CustomerID
    status     Status
    lines      []OrderLine
    events     []DomainEvent
}

func NewOrder(id OrderID, customer CustomerID, lines []OrderLine) (*Order, error) {
    if len(lines) == 0 { return nil, ErrOrderWithoutLines }
    o := &Order{id: id, customerID: customer, status: StatusPending, lines: slices.Clone(lines)}
    o.events = append(o.events, OrderCreated{OrderID: id})
    return o, nil
}

func (o *Order) Confirm() error {
    if o.status != StatusPending { return ErrOrderNotPending }
    o.status = StatusConfirmed
    o.events = append(o.events, OrderConfirmed{OrderID: o.id})
    return nil
}

func (o *Order) ID() OrderID         { return o.id }
func (o *Order) Lines() []OrderLine  { return slices.Clone(o.lines) }
func (o *Order) PullEvents() []DomainEvent { e := o.events; o.events = nil; return e }
```
Unexported fields + constructor + methods are Go's encapsulation; the package (e.g. `internal/domain/order`) is the boundary. Errors are sentinel values (`errors.Is`). The repository maps between the aggregate and DB rows (sqlc/pgx/GORM structs live in infrastructure); add an `Unmarshal`/`Rehydrate` constructor for loading.

## Output format
```
## Detected
<stack, existing base types/conventions reused, ORM>

## Model
<aggregate, value objects, events - full code>

## Invariants enforced
| Rule | Where it's enforced |

## Persistence mapping
<ORM mapping or repository mapping notes>
```

## Principles
- **Encapsulate invariants in the aggregate** - no public setters that let outsiders break rules. Mutate through intention-revealing methods.
- **The aggregate is the consistency boundary** - reference other aggregates by ID, keep aggregates small.
- **Value objects for concepts**, not primitives everywhere.
- Don't model anemically (logic in services, data in entities) - but also don't over-DDD a simple CRUD table.
- Work with the framework's persistence model, not against it; when the ORM forces compromises, isolate them in infrastructure.

## How to use it & best prompts
"Model an Order aggregate with these rules", "turn this anemic entity into a rich aggregate", "extract Money/Email value objects", "add domain events for order lifecycle", "DDD aggregate in TypeScript mapped from Prisma", "rich domain model for my SQLAlchemy app", "aggregate with AbstractAggregateRoot in Spring", "Go aggregate with unexported fields". Pairs with `result-pattern-scaffolder` (method return types), `crud-vs-event-sourcing` (if you're unsure the aggregate needs ES), and `cqrs-setup`.
