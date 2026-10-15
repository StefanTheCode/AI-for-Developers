---
name: specification-pattern-generator
description: Implement the Specification pattern (reusable, composable, testable query criteria) in any stack - EF Core specifications with an evaluator or Ardalis.Specification (.NET), Spring Data JPA Specification + JpaSpecificationExecutor (Java/Kotlin), Prisma where-input builders / TypeORM query-builder functions / Drizzle SQL expressions (TypeScript), SQLAlchemy expression builders and Django Q objects / custom QuerySets (Python), GORM scopes (Go). Use this skill whenever the user wants the specification pattern, reusable query criteria or filters, to dedupe repeated Where/Include/filter logic, composable filters (and/or/not), a generic repository with specs, dynamic search filters, or asks "how do I reuse query logic / specification pattern in my ORM". Always use this skill for specification-pattern requests; it generates the base spec, the evaluator or ORM-native equivalent, and an example.
category: Database
version: 1.0.0
---

# Specification Pattern Generator

Turn repeated query criteria into reusable, composable, unit-testable specification objects (or the ORM's native equivalent), applied to the query in one place.

## Input - point it at your code
Works on a target: a **folder**, **project/repo**, or **GitHub URL**. First find the duplicated query logic to consolidate - specs must target real repetition.

## Step 1 - detect the stack and ORM

| Manifest | ORM | Native mechanism | Find duplicated criteria |
|---|---|---|---|
| `*.csproj` | EF Core | Custom `Specification<T>` + evaluator, or Ardalis.Specification if present | `grep -rn "\.Where(\|\.Include(\|\.OrderBy" --include=*.cs` |
| `pom.xml` / `build.gradle(.kts)` | Spring Data JPA | `Specification<T>` + `JpaSpecificationExecutor` (built in) | `grep -rn "@Query\|findBy\|CriteriaBuilder" src` |
| `package.json` | Prisma / TypeORM / Drizzle | Typed where-input builders / query-builder functions / SQL expressions | `grep -rn "where:\|\.andWhere(\|\.where(" --include=*.ts` |
| `pyproject.toml` / `requirements*.txt` | SQLAlchemy / Django ORM | Functions returning SQL expressions / `Q` objects and custom `QuerySet` methods | `grep -rn "\.filter(\|\.where(\|Q(" --include=*.py` |
| `go.mod` | GORM (or sqlc/squirrel) | GORM scopes `func(*gorm.DB) *gorm.DB` | `grep -rn "\.Where(\|\.Scopes(" --include=*.go` |

## Universal principles (every stack)
1. **Use specs to dedupe real repetition** - don't wrap a one-off query in ceremony.
2. **A spec is a named business rule** (`ActiveOrdersForCustomer`, `OverdueInvoices`), not a generic "filter by field X" wrapper.
3. **Composable:** `and` / `or` / `not` so rules combine without new classes for every combination.
4. **Translated to SQL**, never evaluated in memory over a loaded list. Only use expressions the ORM can translate.
5. **Unit-testable:** test the rule against in-memory objects (where the stack can evaluate the predicate) and once against a real DB.
6. **Specs don't hide performance problems** - they still need sensible eager loading/projection and indexes on the filtered columns.
7. **Read specs default to read-only / no tracking** where the ORM has that concept.
8. **Prefer the ORM's native mechanism** over a hand-rolled framework when one exists (Spring Data `Specification`, Django `QuerySet`, GORM scopes).

## C# / .NET - EF Core
If Ardalis.Specification is already referenced, use it. Otherwise:
```csharp
public abstract class Specification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; protected set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public int? Take { get; private set; }
    public int? Skip { get; private set; }
    public bool AsNoTracking { get; private set; } = true;

    protected void Where(Expression<Func<T, bool>> c) => Criteria = c;
    protected void Include(Expression<Func<T, object>> i) => Includes.Add(i);
    protected void Order(Expression<Func<T, object>> o) => OrderBy = o;
    protected void Page(int skip, int take){ Skip = skip; Take = take; }
    protected void Tracked() => AsNoTracking = false;
}

public static class SpecificationEvaluator
{
    public static IQueryable<T> Apply<T>(IQueryable<T> q, Specification<T> spec) where T : class
    {
        if (spec.AsNoTracking) q = q.AsNoTracking();
        if (spec.Criteria is not null) q = q.Where(spec.Criteria);
        q = spec.Includes.Aggregate(q, (cur, inc) => cur.Include(inc));
        if (spec.OrderBy is not null) q = q.OrderBy(spec.OrderBy);
        if (spec.Skip is not null) q = q.Skip(spec.Skip.Value);
        if (spec.Take is not null) q = q.Take(spec.Take.Value);
        return q;
    }
}

public class ActiveOrdersForCustomer : Specification<Order>
{
    public ActiveOrdersForCustomer(Guid customerId)
    {
        Where(o => o.CustomerId == customerId && o.Status == OrderStatus.Active);
        Include(o => o.Lines);
        Order(o => o.CreatedAt);
    }
}

var orders = await SpecificationEvaluator
    .Apply(db.Orders, new ActiveOrdersForCustomer(id)).ToListAsync(ct);
```
Unit test: `spec.Criteria!.Compile()(order)` against in-memory entities. For `And`/`Or` composition of expression criteria, combine with a parameter-rebinding `ExpressionVisitor` (or use the library's support).

## Java / Kotlin - Spring Data JPA
```java
public interface OrderRepository extends JpaRepository<Order, UUID>, JpaSpecificationExecutor<Order> {}

public final class OrderSpecs {
    private OrderSpecs() {}
    public static Specification<Order> forCustomer(UUID customerId) {
        return (root, query, cb) -> cb.equal(root.get("customerId"), customerId);
    }
    public static Specification<Order> active() {
        return (root, query, cb) -> cb.equal(root.get("status"), OrderStatus.ACTIVE);
    }
}

var page = orders.findAll(
    OrderSpecs.forCustomer(id).and(OrderSpecs.active()),
    PageRequest.of(0, 20, Sort.by("createdAt")));
```
Use the JPA static metamodel (`Order_.status`) instead of strings if the project generates it. Eager loading: add an `@EntityGraph` on the repository method or fetch in the spec only for non-count queries. Querydsl `Predicate`s are the equivalent if the project uses Querydsl.

## TypeScript
**Prisma** - specs are typed `WhereInput` builders, composed with `AND`/`OR`:
```ts
export const activeOrders = (): Prisma.OrderWhereInput => ({ status: "ACTIVE" });
export const forCustomer = (customerId: string): Prisma.OrderWhereInput => ({ customerId });

const orders = await prisma.order.findMany({
  where: { AND: [activeOrders(), forCustomer(id)] },
  include: { lines: true },
  orderBy: { createdAt: "asc" },
});
```
**Drizzle** - specs return `SQL` expressions combined with `and()` / `or()`: `const activeOrders = () => eq(orders.status, "ACTIVE")`, then `db.select().from(orders).where(and(activeOrders(), eq(orders.customerId, id)))`.
**TypeORM** - specs as functions `(qb: SelectQueryBuilder<Order>) => SelectQueryBuilder<Order>` applied in sequence, or `FindOptionsWhere<Order>` objects for simple cases. Keep parameter names unique per spec to avoid collisions.

## Python
**SQLAlchemy** - specs return boolean column expressions:
```python
def active_orders() -> ColumnElement[bool]:
    return Order.status == OrderStatus.ACTIVE

def for_customer(customer_id: UUID) -> ColumnElement[bool]:
    return Order.customer_id == customer_id

stmt = (select(Order)
        .where(and_(active_orders(), for_customer(cid)))
        .options(selectinload(Order.lines))
        .order_by(Order.created_at))
orders = (await session.scalars(stmt)).all()
```
**Django** - the idiomatic spec is a custom `QuerySet` method (chainable) or a `Q` object (composable with `&`, `|`, `~`):
```python
class OrderQuerySet(models.QuerySet):
    def active(self):              return self.filter(status=OrderStatus.ACTIVE)
    def for_customer(self, cid):   return self.filter(customer_id=cid)

class Order(models.Model):
    objects = OrderQuerySet.as_manager()

Order.objects.active().for_customer(cid).prefetch_related("lines").order_by("created_at")
```

## Go - GORM scopes
```go
func Active(db *gorm.DB) *gorm.DB { return db.Where("status = ?", StatusActive) }

func ForCustomer(id string) func(*gorm.DB) *gorm.DB {
    return func(db *gorm.DB) *gorm.DB { return db.Where("customer_id = ?", id) }
}

func Paginate(page, size int) func(*gorm.DB) *gorm.DB {
    return func(db *gorm.DB) *gorm.DB { return db.Offset((page - 1) * size).Limit(size) }
}

var orders []Order
err := db.WithContext(ctx).Scopes(Active, ForCustomer(id), Paginate(1, 20)).
    Preload("Lines").Order("created_at").Find(&orders).Error
```
With sqlc there is no runtime composition - write one named query per rule; with squirrel, specs are `sq.Sqlizer` values combined with `sq.And{...}`.

## Output format
```
## Detected
<stack, ORM, mechanism chosen (native vs custom) and why>

## Duplication found
| Criteria | Occurrences (file:line) | Spec name |

## Files
<base spec/evaluator (if needed), the specs, the refactored call sites>

## Tests
<a unit test per spec, plus one DB-backed test>
```

## How to use it & best prompts
"Add the specification pattern", "dedupe these repeated EF queries into specs", "make my query criteria reusable and testable", "Spring Data Specifications for the order search", "reusable Prisma filters for orders", "custom QuerySet methods instead of repeated filters", "GORM scopes for these repeated Where clauses". Pairs with `orm-query-optimizer`.
