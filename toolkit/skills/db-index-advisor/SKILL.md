---
name: db-index-advisor
description: Recommend database indexes based on how tables/entities are actually queried - filters, joins, ordering, uniqueness - and generate the index definition in the project's own format (EF Core HasIndex, Prisma @@index, TypeORM/Drizzle index, SQLAlchemy Index / Django Meta.indexes, JPA @Index, GORM index tags, or a plain SQL migration). Use this skill whenever the user wants index recommendations, asks why a query is slow due to a missing index, wants composite/covering/unique/partial indexes, or asks "what indexes do I need / how do I index this". Works for PostgreSQL, SQL Server, MySQL/MariaDB, and SQLite. Always use this skill for indexing requests; it maps query patterns to concrete index definitions without over-indexing.
category: Database
version: 1.0.0
---

# Database Index Advisor

Find the indexes a schema is missing by looking at how it is queried, and emit the index definition (column order, includes, uniqueness, filter) in the project's own format - without over-indexing.

## Input - point it at your code
Works on a target: a **file**, **folder**, **project/repo**, or **GitHub URL** (clone first: `git clone --depth 1 <url> /tmp/repo`). Report which files you scanned and group recommendations by table.

## Step 1 - detect the stack, ORM, and database

| Manifest | Look for | Where indexes are declared |
|---|---|---|
| `*.csproj` | `Microsoft.EntityFrameworkCore.*` | `HasIndex(...)` in `OnModelCreating` / `IEntityTypeConfiguration<T>`, or `[Index]` attribute |
| `package.json` | `prisma`, `typeorm`, `drizzle-orm`, `knex` | `@@index` / `@@unique` in `schema.prisma`; `@Index()` on TypeORM entities; `index()` in Drizzle table config; Knex migrations |
| `pyproject.toml`, `requirements*.txt` | `sqlalchemy`, `alembic`, `django` | `Index(...)` / `index=True` on `Column`; Django `Meta.indexes` / `db_index=True` |
| `pom.xml`, `build.gradle(.kts)` | `spring-boot-starter-data-jpa`, `flyway`, `liquibase` | `@Table(indexes = @Index(...))`, but prefer Flyway/Liquibase SQL for production |
| `go.mod` | `gorm.io/gorm`, `golang-migrate`, `goose` | GORM `index` / `uniqueIndex` struct tags, or SQL migrations |

Detect the database from the connection/provider (`UseNpgsql`, `provider = "postgresql"`, `postgresql+psycopg`, `jdbc:sqlserver`, `gorm.io/driver/mysql`). Index features differ per engine - see the matrix below.

## Step 2 - find query patterns and existing indexes
```
# .NET
grep -rn "\.Where(\|\.OrderBy\|\.Join(\|\.FirstOrDefault\|\.Single" --include=*.cs <target>
grep -rn "HasIndex\|IsUnique\|\[Index(" --include=*.cs <target>
# Node / TypeScript
grep -rn "where:\|orderBy:\|\.where(\|\.orderBy(" --include=*.ts <target> | grep -v node_modules
grep -rn "@@index\|@@unique\|@Index\|index(" <target>/prisma <target>/src
# Python
grep -rn "\.filter(\|\.filter_by(\|\.order_by(\|\.where(" --include=*.py <target>
grep -rn "Index(\|index=True\|db_index\|Meta.indexes\|indexes = \[" --include=*.py <target>
# Java / Kotlin
grep -rn "findBy\|@Query\|ORDER BY\|WHERE" <target>/src
grep -rn "@Index\|CREATE INDEX\|createIndex" <target>/src
# Go
grep -rn "\.Where(\|\.Order(\|WHERE\|ORDER BY" --include=*.go <target>
grep -rn "index:\|uniqueIndex\|CREATE INDEX" <target>
# Raw SQL migrations, any stack
grep -rni "create \(unique \)\?index" <target>
```
Focus on the hot paths: list endpoints, search, dashboards, background jobs, and foreign keys used in joins.

## How it reasons (universal)
For each frequently-queried table, map predicates to an index:
- **Equality filters** - index the filtered column(s). Several `AND` equality filters - **composite** index (most common / most selective first).
- **Range + equality** - equality columns first, range column last.
- **`ORDER BY` after a filter** - append the sort column to the composite so the engine can skip the sort.
- **Frequently selected extra columns** - consider a **covering** index (`INCLUDE` on Postgres 11+ / SQL Server).
- **Natural keys / dedupe** - **unique** index.
- **Filters on a fixed subset** (`WHERE deleted_at IS NULL`, `status = 'active'`) - **partial / filtered** index where the engine supports it.
- **Foreign keys used in joins/filters** - index them. EF Core and Django create FK indexes by default and MySQL/InnoDB requires one; PostgreSQL and SQL Server do not create them, and Prisma, SQLAlchemy, Hibernate, and GORM usually won't add them for you - check.
- **Leading wildcard `LIKE '%x%'`** - a B-tree won't help; suggest full-text or trigram (`pg_trgm`) and say so.
- **Functions on the column** (`LOWER(email)`) - expression/computed index, or normalize the stored value.

## Engine capability matrix

| Feature | PostgreSQL | SQL Server | MySQL / MariaDB | SQLite |
|---|---|---|---|---|
| Covering (`INCLUDE`) | yes (11+) | yes | no (add columns to key) | no |
| Partial / filtered | `WHERE ...` | `WHERE ...` (filtered) | no | `WHERE ...` |
| Expression index | yes | via computed column | yes (8.0.13+ functional) | yes |
| Online build | `CREATE INDEX CONCURRENTLY` | `WITH (ONLINE = ON)` (edition-dependent) | InnoDB online DDL (`ALGORITHM=INPLACE`) | n/a |

## Per-stack output

### C# / .NET (EF Core)
```csharp
modelBuilder.Entity<Order>()
    .HasIndex(o => new { o.CustomerId, o.Status, o.CreatedAt })   // filter CustomerId+Status, sort CreatedAt
    .HasDatabaseName("IX_Orders_Customer_Status_Created")
    .IncludeProperties(o => o.Total);                              // covering (SQL Server / Npgsql)

modelBuilder.Entity<Order>()
    .HasIndex(o => o.ExternalRef).IsUnique();

modelBuilder.Entity<Order>()
    .HasIndex(o => o.Status).HasFilter("\"DeletedAt\" IS NULL");   // partial; SQL text is provider-specific
```
Verify with `query.ToQueryString()` plus the actual execution plan / `EXPLAIN ANALYZE`.

### TypeScript / Node
```prisma
model Order {
  // ...
  @@index([customerId, status, createdAt])
  @@unique([externalRef])
}
```
TypeORM: `@Index(["customerId", "status", "createdAt"])` on the entity. Drizzle: `index("ix_orders_cust_status_created").on(t.customerId, t.status, t.createdAt)` in the table's extra config. For partial/`INCLUDE`/concurrent builds, hand-edit the generated SQL migration.

### Python
SQLAlchemy: `Index("ix_orders_cust_status_created", Order.customer_id, Order.status, Order.created_at)` in `__table_args__`; Postgres partial via `postgresql_where=`, covering via `postgresql_include=`. Alembic: `op.create_index(..., postgresql_concurrently=True)` inside an autocommit block. Django: `Meta.indexes = [models.Index(fields=["customer", "status", "-created_at"], name="...", condition=Q(deleted_at__isnull=True), include=["total"])]`; `AddIndexConcurrently` for Postgres.

### Java / Kotlin (JPA / Spring)
`@Table(indexes = @Index(name = "ix_orders_cust_status_created", columnList = "customer_id, status, created_at"))` documents intent, but schema generation should not run in production - ship the index as a Flyway/Liquibase SQL migration.

### Go
GORM: `gorm:"index:ix_orders_cust_status_created,priority:1"` on each column (priority sets order), `uniqueIndex` for uniqueness. With golang-migrate/goose, write the `CREATE INDEX` SQL directly.

## Output
```
## Summary
<stack + ORM + database detected; headline recommendations>

## Recommended indexes
1. <table>(<cols>) [composite|unique|covering|partial] - serves <query at file:line> - <why this column order>
2. ...

## Index definitions
<code in the project's own format, ready to paste>

## Redundant / unused candidates
- <index> - covered by <composite> (verify usage stats before dropping)

## Verify on your side
- <EXPLAIN / execution plan to run; write-heavy tables where the cost matters>
```

## Principles
- **Index for the queries you run, not every column.** Every index speeds reads but slows writes and costs storage.
- **Column order in composites matters** - equality before range, common filters first.
- **Confirm with the query plan.** Recommend `EXPLAIN ANALYZE` (Postgres), `EXPLAIN` (MySQL/SQLite), actual execution plan (SQL Server), and logging the generated SQL.
- Watch for redundant indexes (a composite often makes a single-column one on its leading column unnecessary).
- On large live tables, build indexes online/concurrently - pair with `db-migration-reviewer`.

## How to use it & best prompts
"What indexes does this project need?", "why is this query slow - missing index?", "design a composite index for filtering by customer+status and sorting by date", "add the Prisma indexes for these queries". Pairs with `orm-query-optimizer` and `db-migration-reviewer`.
