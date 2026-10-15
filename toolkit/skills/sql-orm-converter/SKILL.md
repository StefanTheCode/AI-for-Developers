---
name: sql-orm-converter
description: Convert raw SQL to ORM query code and ORM queries back to the SQL they generate, both directions, preserving semantics - EF Core LINQ, Prisma, Drizzle, TypeORM QueryBuilder, Knex, SQLAlchemy 2.0 select(), Django QuerySet, JPA Criteria / JPQL / Spring Data, jOOQ, and GORM. Use this skill whenever the user wants to turn a SQL query into LINQ/Prisma/SQLAlchemy/JPQL/GORM code, convert ORM code to SQL, migrate stored procedures or raw ADO.NET/JDBC/driver queries to an ORM, port a query from one ORM to another, or asks "how do I write this SQL in EF / Prisma / Django / what SQL does this query produce / convert this query". Always use this skill for SQL-ORM conversion; it preserves behavior and flags anything that won't translate.
category: Database
version: 1.0.0
---

# SQL - ORM Converter

Translate between raw SQL and ORM query code in either direction (or between two ORMs), keeping the result set identical and flagging constructs that don't translate cleanly.

## Input - point it at your code or paste the query
Works on a target (a **file**, **folder**, **project/repo**, **GitHub URL**) or a pasted query. For SQL -> ORM you need the model/entity definitions and relationships - find them before writing code.

## Step 1 - detect the target ORM

| Manifest | Look for | Target API | Find the models |
|---|---|---|---|
| `*.csproj` | `Microsoft.EntityFrameworkCore` | LINQ (method syntax) | `grep -rn "DbSet<\|class .*Configuration" --include=*.cs` |
| `package.json` | `@prisma/client`, `drizzle-orm`, `typeorm`, `knex` | Prisma Client / Drizzle query builder / TypeORM QueryBuilder / Knex | `schema.prisma`, `pgTable(`/`mysqlTable(`, `@Entity` |
| `pyproject.toml`, `requirements*.txt` | `sqlalchemy`, `django` | SQLAlchemy 2.0 `select()` / Django QuerySet | `DeclarativeBase`/`Mapped[`, `models.Model` |
| `pom.xml`, `build.gradle(.kts)` | `spring-boot-starter-data-jpa`, `jooq` | JPQL / Criteria API / Spring Data derived queries / jOOQ | `@Entity`, `JpaRepository` |
| `go.mod` | `gorm.io/gorm`, `sqlc`, `sqlx` | GORM chain / sqlc queries / sqlx | struct tags, `query.sql` |

If the user names a target ("in Prisma"), use it. Also note the database dialect - it decides `LIMIT`/`TOP`, `ILIKE`, date functions, and boolean literals.

## Workflow
1. **Parse the SQL** into: sources and joins (inner/left), filters, grouping/having, projection, ordering, paging, subqueries/CTEs.
2. **Map joins to relationships** where the model defines them (navigation properties, relations); fall back to explicit joins only where no relation exists.
3. **Write the ORM code** in the project's idiom, projecting only the selected columns.
4. **Check semantics** - see the checklist; call out any difference.
5. **Show the generated SQL** (or how to get it) so the translation is verifiable.

## Semantics checklist (universal)
- **LEFT vs INNER join** - a relation-based count/aggregate is a LEFT join; a filter on the joined table turns it into an INNER join.
- **NULL handling** - `= NULL` never matches; `NOT IN` with NULLs returns nothing; ORMs differ in how they translate `!=` against nullable columns.
- **Aggregates over empty sets** - `SUM` returns NULL, not 0; `COUNT(col)` skips NULLs, `COUNT(*)` does not.
- **GROUP BY / HAVING** - every non-aggregated projected column must be grouped; `HAVING` becomes a filter after grouping.
- **DISTINCT and duplicates** introduced by joining to a collection.
- **Ordering + paging** - paging without a stable `ORDER BY` is nondeterministic.
- **Case sensitivity and collation** - `LIKE` vs `ILIKE`, DB collation vs in-memory comparison.
- **Client-side evaluation** - anything the ORM can't translate may silently run in memory (or throw). Flag it.

## SQL -> ORM example
```sql
SELECT c.name, COUNT(o.id) AS orders
FROM customers c LEFT JOIN orders o ON o.customer_id = c.id
WHERE c.is_active = TRUE
GROUP BY c.id, c.name HAVING COUNT(o.id) > 5
ORDER BY orders DESC;
```

**C# / EF Core (LINQ)**
```csharp
var rows = await db.Customers
    .Where(c => c.IsActive)
    .Select(c => new { c.Name, Orders = c.Orders.Count() })
    .Where(x => x.Orders > 5)
    .OrderByDescending(x => x.Orders)
    .ToListAsync(ct);
```
See the SQL with `query.ToQueryString()` or `LogTo`. Raw SQL when needed: `FromSql($"... {p}")` (interpolation is parameterized) or `Database.SqlQuery<T>(...)` for unmapped types.

**TypeScript - Prisma / Drizzle**
Prisma has no `HAVING` on relation counts in `findMany`; use `groupBy` with `having` on the FK table, or a typed raw query:
```ts
const rows = await prisma.$queryRaw<{ name: string; orders: bigint }[]>`
  SELECT c.name, COUNT(o.id) AS orders FROM customers c
  LEFT JOIN orders o ON o.customer_id = c.id
  WHERE c.is_active = TRUE GROUP BY c.id, c.name HAVING COUNT(o.id) > 5 ORDER BY orders DESC`;
```
```ts
// Drizzle
const orderCount = count(orders.id);
const rows = await db.select({ name: customers.name, orders: orderCount })
  .from(customers).leftJoin(orders, eq(orders.customerId, customers.id))
  .where(eq(customers.isActive, true))
  .groupBy(customers.id, customers.name)
  .having(gt(orderCount, 5))
  .orderBy(desc(orderCount));
```
TypeORM: `createQueryBuilder("c").leftJoin("c.orders", "o").select(...).addSelect("COUNT(o.id)", "orders").where(...).groupBy(...).having("COUNT(o.id) > :n", { n: 5 }).getRawMany()`.

**Python - SQLAlchemy 2.0 / Django**
```python
order_count = func.count(Order.id).label("orders")
stmt = (select(Customer.name, order_count)
        .outerjoin(Customer.orders)
        .where(Customer.is_active.is_(True))
        .group_by(Customer.id, Customer.name)
        .having(func.count(Order.id) > 5)
        .order_by(order_count.desc()))
rows = (await session.execute(stmt)).all()
```
```python
# Django
rows = (Customer.objects.filter(is_active=True)
        .annotate(orders=Count("orders"))
        .filter(orders__gt=5).order_by("-orders").values("name", "orders"))
```
SQL: `print(stmt)` / `stmt.compile(dialect=...)`; Django `str(qs.query)`.

**Java / Kotlin - JPQL / Spring Data**
```java
@Query("""
  select new com.acme.CustomerOrders(c.name, count(o))
  from Customer c left join c.orders o
  where c.active = true
  group by c.id, c.name having count(o) > 5
  order by count(o) desc""")
List<CustomerOrders> activeWithManyOrders();
```
Criteria API for dynamic filters (or Spring Data `Specification`); jOOQ when the team prefers type-safe SQL. SQL: `spring.jpa.show-sql` / Hibernate SQL logging.

**Go - GORM**
```go
var rows []struct{ Name string; Orders int64 }
err := db.WithContext(ctx).Model(&Customer{}).
    Select("customers.name, COUNT(orders.id) AS orders").
    Joins("LEFT JOIN orders ON orders.customer_id = customers.id").
    Where("customers.is_active = ?", true).
    Group("customers.id, customers.name").
    Having("COUNT(orders.id) > ?", 5).
    Order("orders DESC").Scan(&rows).Error
```
SQL: `db.Debug()` or `db.ToSQL(...)`. For SQL-first Go teams, sqlc generates typed code from the original query - often the better answer.

## ORM -> SQL
Show the SQL the ORM will produce, using its own tooling (`ToQueryString()`, Prisma `log: ['query']`, `str(qs.query)`, Hibernate SQL logging, `db.ToSQL`), and point out N+1 patterns, client-side evaluation, and missing `ORDER BY` on paged queries.

## Won't-translate flags
- **Window functions, CTEs, recursive queries, `PIVOT`, `LATERAL`** - support varies (SQLAlchemy, jOOQ, and Drizzle handle many; EF Core, Prisma, and GORM mostly don't). Use the ORM's raw SQL escape hatch or a mapped view, and say so.
- **Vendor-specific functions** - map to `EF.Functions.*`, SQLAlchemy `func.*`, Drizzle's `sql` template, Django `Func` expressions, or JPQL `function(...)` where available; else raw SQL.
- **Set operations** (`UNION`/`INTERSECT`/`EXCEPT`) - check ORM support before promising it.
- **Stored procedures** - call them through the raw SQL API (`FromSql`, `$queryRaw`, `session.execute(text(...))`, `@Procedure`/`StoredProcedureQuery`, `db.Raw`), or port the logic if simple.
- **Locking hints** (`FOR UPDATE`, `WITH (NOLOCK)`) - map to the ORM's lock API where it exists, else raw SQL.

## Output
```
## Target
<ORM + dialect detected>

## Converted query
<code, ready to paste>

## Generated SQL (expected)
<SQL, or the command to print it>

## Semantics notes
- <any difference in NULL/join/aggregate/collation behavior; anything that falls back to raw SQL>
```

## Principles
- **Preserve semantics first** (joins, null handling, grouping), performance second.
- **Parameterize** - never concatenate user input into raw SQL; use the ORM's parameterized raw API.
- When a query genuinely belongs in SQL (heavy analytics, reporting), say so instead of forcing awkward ORM code.
- Always show or offer the generated SQL so the translation is verifiable.

## How to use it & best prompts
"Convert this SQL to EF Core LINQ", "write this query in Prisma / Drizzle", "port this stored procedure to SQLAlchemy", "what SQL does this Django QuerySet generate", "turn this native query into JPQL", "rewrite this raw query with GORM". Pairs with `orm-query-optimizer`.
