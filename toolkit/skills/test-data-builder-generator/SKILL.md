---
name: test-data-builder-generator
description: Generate test data builders, factories, and object mothers for test suites in any stack so tests create valid domain objects without brittle, repetitive setup - C#/.NET (fluent builders, Bogus, AutoFixture), TypeScript/JavaScript (fishery, @faker-js/faker), Python (factory_boy, Faker, polyfactory), Java/Kotlin (builders, Instancio, Datafaker), Go (builder/functional options, gofakeit). Use this skill whenever the user wants test data builders, object mothers, factories, fixtures, fake data, a fluent builder for test objects, less duplicated test setup, or asks "how do I build test data / reduce test setup boilerplate / AutoFixture vs builders / factory_boy vs fixtures". Always use this skill for test-data-builder requests; it generates builders matching the domain.
category: Testing
version: 1.0.0
---

# Test Data Builder Generator

Generate builders/factories (and object mothers) so tests construct valid domain objects in one readable line, with only the fields that matter to each test made explicit.

## Input - point it at your code

Works on a target: a **class/file**, **folder**, **project**, or **GitHub URL** (clone first: `git clone --depth 1 <url> /tmp/repo`). Read the domain types and the existing tests to see the real setup pain: repeated constructor calls / object literals, copy-pasted fixtures, invalid objects built by bypassing factories.

## Step 1 - detect the stack and what's already used

| Manifest | Stack | Hand-written pattern | Libraries (use if present or a good fit) |
|---|---|---|---|
| `*.csproj` | C# / .NET | fluent builder + object mother | Bogus, AutoFixture |
| `package.json` | TS / JS | factory function with overrides | fishery, @faker-js/faker |
| `pyproject.toml`, `requirements*.txt` | Python | factory function / pytest fixture | factory_boy, Faker, polyfactory (Pydantic/dataclasses) |
| `pom.xml`, `build.gradle(.kts)` | Java / Kotlin | builder class (Java), data class defaults + `copy` (Kotlin) | Instancio, Datafaker |
| `go.mod` | Go | builder or functional options returning a struct | gofakeit |

Find setup duplication, e.g.:
```
grep -rn "new Order(\|new Order {" --include=*Tests.cs <target>
grep -rnE "const \w+ = \{|new \w+\(" --include=*.{test,spec}.ts <target> | grep -v node_modules
grep -rnE "^\s+\w+ = \w+\(" --include=test_*.py <target>
```
If the project already uses a library (Bogus, fishery, factory_boy, Instancio), extend that rather than introducing another.

## Universal rules

1. **Default everything, override what the test cares about.** The test body should show only the values that drive its assertion.
2. **Build valid objects** - go through the domain's constructors/factories so invariants hold. Invalid objects are built on purpose by a named method (`WithoutLines()`), never by accident.
3. **Name canonical scenarios** (object mothers / traits): `Orders.Confirmed()`, `orderFactory.confirmed()`, `OrderFactory(confirmed=True)`.
4. **Unique where it matters** - sequences or random IDs for keys/emails so tests don't collide in a shared database.
5. **Deterministic by default** - seed faker libraries so a failing test reproduces; random values only where the test truly doesn't care.
6. **Relationships explicit** - a builder for the aggregate composes child builders; don't hide large object graphs behind magic.
7. **Separate build from persist** - in-memory `build()` for unit tests; `create()`/save helpers only for integration tests.

## C# / .NET

```csharp
public class OrderBuilder
{
    private Guid _customerId = Guid.NewGuid();
    private OrderStatus _status = OrderStatus.Pending;
    private readonly List<OrderLine> _lines = new() { new("SKU1", 1) };

    public OrderBuilder ForCustomer(Guid id) { _customerId = id; return this; }
    public OrderBuilder WithStatus(OrderStatus s) { _status = s; return this; }
    public OrderBuilder WithLines(params OrderLine[] l) { _lines.Clear(); _lines.AddRange(l); return this; }

    public Order Build() => Order.Create(_customerId, _lines).Value with { Status = _status };
    public static implicit operator Order(OrderBuilder b) => b.Build();
}

public static class Orders   // object mother
{
    public static Order Pending()   => new OrderBuilder();
    public static Order Confirmed() => new OrderBuilder().WithStatus(OrderStatus.Confirmed);
}

// test: only what matters is explicit
Order order = new OrderBuilder().WithStatus(OrderStatus.Confirmed);
```
- **Bogus** (`Faker<T>` with `RuleFor`, `UseSeed(...)`) for realistic values in bulk; use `CustomInstantiator` to go through the domain factory.
- **AutoFixture** for "I don't care about the values" cases (`fixture.Build<T>().With(...).Create()`); combine with builders for the parts you do care about. Can fight private setters and invariants - prefer builders for rich domain types.

## TypeScript / JavaScript

```ts
import { Factory } from "fishery";
import { faker } from "@faker-js/faker";

class OrderFactory extends Factory<Order> {
  confirmed() { return this.params({ status: "confirmed" }); }
}

export const orderFactory = OrderFactory.define(({ sequence }) => ({
  id: `order-${sequence}`,
  customerId: faker.string.uuid(),
  status: "pending",
  lines: [{ sku: "SKU1", qty: 1 }],
}));

const order = orderFactory.confirmed().build({ customerId: "c1" });
```
Without a library, a factory function `makeOrder(overrides: Partial<Order> = {}): Order => ({ ...defaults, ...overrides })` is often enough. Call `faker.seed(n)` for reproducible runs. If `Order` is a class with invariants, build through its constructor/factory (fishery's `onCreate`/after-build hooks or a plain function).

## Python

```python
import factory

class OrderFactory(factory.Factory):
    class Meta:
        model = Order

    id = factory.Sequence(lambda n: n + 1)
    customer_id = factory.Faker("uuid4")
    status = "pending"
    lines = factory.LazyFunction(lambda: [OrderLine("SKU1", 1)])

    class Params:
        confirmed = factory.Trait(status="confirmed")

order = OrderFactory(confirmed=True, customer_id="c1")
```
Use `factory.django.DjangoModelFactory` / `factory.alchemy.SQLAlchemyModelFactory` when persisting in integration tests; `OrderFactory.build()` stays in memory. For Pydantic models or dataclasses, `polyfactory` generates valid instances from type hints. Expose common objects as pytest fixtures that call the factory.

## Java / Kotlin

- **Java:** a builder class with defaults and `withX(...)` methods whose `build()` calls the domain constructor/factory; an object mother class with static named scenarios. **Instancio** fills whole object graphs (`Instancio.of(Order.class).set(field(Order::getStatus), CONFIRMED).create()`) for "don't care" data; **Datafaker** for realistic values.
- **Kotlin:** a top-level `fun anOrder(status: OrderStatus = PENDING, lines: List<OrderLine> = listOf(defaultLine())) = Order.create(...)` with default arguments usually replaces a builder; `copy(...)` for variants of data classes.

## Go

```go
type OrderBuilder struct{ o Order }

func AnOrder() *OrderBuilder {
	return &OrderBuilder{o: Order{ID: uuid.NewString(), Status: StatusPending, Lines: []Line{{SKU: "SKU1", Qty: 1}}}}
}
func (b *OrderBuilder) WithStatus(s Status) *OrderBuilder { b.o.Status = s; return b }
func (b *OrderBuilder) Build() Order                      { return b.o }

order := AnOrder().WithStatus(StatusConfirmed).Build()
```
Functional options (`NewTestOrder(WithStatus(StatusConfirmed))`) work equally well. Use `gofakeit` for realistic values and seed it for reproducibility. If the type has a validating constructor, `Build()` should call it and `t.Fatal` on error via a helper taking `testing.TB`.

## Output format

```
## Detected
<stack, existing test data approach, library recommendation and why>

## Builders / factories
<generated code, one per type, plus object mother / traits>

## Before / after
<one existing test rewritten to use the builder>

## Notes
<invariants respected, seeding, what still needs persistence helpers>
```

## Builders vs generated data

- **Builders/factories/mothers** - explicit, readable, domain-aware; best when objects have invariants and relationships.
- **Auto-generated data** (AutoFixture, Instancio, polyfactory) - great for "values don't matter" cases; combine with builders for the parts that do.
The skill recommends based on how much each test depends on specific values.

## Principles

- **Default everything, override what the test cares about** - that's what makes the test's intent obvious.
- Builders produce **valid** objects (respect invariants/factories), not bypass them.
- Name canonical scenarios to kill duplication across tests.

## How to use it & best prompts

"Generate a test data builder for Order", "fishery factories for these types", "factory_boy factories for my Django models", "reduce my test setup boilerplate", "object mothers for these domain types", "builders vs AutoFixture for this". Pairs with `unit-test-generator` and `integration-test-setup`.
