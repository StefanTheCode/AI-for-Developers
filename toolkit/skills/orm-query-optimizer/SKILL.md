---
name: orm-query-optimizer
description: Analyze and optimize ORM and database-access code in any stack - EF Core, Prisma, TypeORM, Drizzle, SQLAlchemy, Django ORM, Hibernate/JPA, Spring Data, GORM. Use this skill whenever the user shares data-access code, a repository, a model/entity, or a slow ORM/database query and wants it reviewed or sped up, or mentions N+1, lazy loading, over-fetching, eager loading, include/select_related/prefetch_related/JOIN FETCH/Preload, AsNoTracking, cartesian explosion, pagination, missing indexes, "this Prisma query is slow", "this EF query is slow", or "why is my query slow". Always use this skill for ORM performance questions instead of answering from memory; it applies a fixed checklist and rewrites the code.
category: Database
---

# ORM Query Optimizer

Find and fix the performance problems in ORM-based data access, then return the optimized code with a short explanation of every change. The problems are universal; only the syntax differs per ORM.

## Input - point it at your code

You don't need pasted snippets. Work directly from whatever target the user gives:

- **A single file**, **a folder**, or **a whole project/repo**
- **A GitHub URL** - clone first, then scan: `git clone --depth 1 <url> /tmp/repo && cd /tmp/repo`

## Step 1 - detect the stack and ORM

Read the manifest before reading queries:

| Manifest | Look for | ORM | Reference to load |
|---|---|---|---|
| `*.csproj` | `Microsoft.EntityFrameworkCore` | EF Core | `references/ef-core-patterns.md` |
| `package.json` | `@prisma/client`, `typeorm`, `drizzle-orm` | Prisma / TypeORM / Drizzle | `references/prisma-typeorm-patterns.md` |
| `pyproject.toml`, `requirements*.txt` | `sqlalchemy`, `django` | SQLAlchemy / Django ORM | `references/sqlalchemy-django-patterns.md` |
| `pom.xml`, `build.gradle(.kts)` | `hibernate`, `spring-boot-starter-data-jpa` | Hibernate / JPA / Spring Data | `references/hibernate-jpa-patterns.md` |
| `go.mod` | `gorm.io/gorm` | GORM | `references/gorm-patterns.md` |

Load only the reference for the detected ORM, and only when the checklist below isn't enough. For other data-access libraries (Dapper, Knex, sqlx, jOOQ, ActiveRecord, Eloquent) apply the universal checklist with that library's idioms.

Then locate candidate queries, e.g.:
```
# .NET
grep -rn "\.Include(\|\.ToListAsync\|AsNoTracking\|DbContext" --include=*.cs <target> | grep -v "/obj/\|/bin/"
# Node / TypeScript
grep -rn "\.findMany\|\.findUnique\|getRepository\|createQueryBuilder\|db\.select\|db\.query\." --include=*.ts <target> | grep -v node_modules
# Python
grep -rn "\.objects\.\|session\.\(query\|execute\|scalars\)\|select(" --include=*.py <target>
# Java / Kotlin
grep -rn "@OneToMany\|@ManyToOne\|FetchType\|@Query\|JpaRepository" <target>/src
# Go
grep -rn "\.Preload(\|\.Find(\|\.First(\|db\.Where" --include=*.go <target>
```
On a large project, scan the matched files, **prioritize the worst offenders** (loops, list endpoints, dashboards, reports), and skip unrelated code. Tell the user which files you scanned and group findings by file.

## Workflow

1. **Read the code and the data shape.** Models/entities, relationships (1:1, 1:many, many:many), and what the caller actually needs.
2. **Run the universal checklist** against every query.
3. **Rewrite** the problematic queries in the ORM's own idiom. Keep behavior identical unless you found a bug (call bugs out separately).
4. **Explain each change** in one line - what was wrong, why it's slow, what the fix does.
5. **Flag what you cannot verify** (indexes need the real schema and query plan; mark them "verify").

## The universal checklist

### 1. N+1 queries
A loop (or lazy relation access) that fires one query per row. The classic killer.
- **Detect:** relation accessed inside a loop; lazy loading enabled; a query call inside `for` / `map` / `forEach` / list comprehension.
- **Fix:** eager-load the relation, batch-load by IDs (`WHERE id IN (...)`), or project everything in one query.

### 2. Over-fetching (no projection)
Loading full entities/rows when only a few columns are used.
- **Fix:** select only the needed columns into a DTO / plain object. Projection usually also removes the need for eager loading and change tracking.

### 3. Tracking / identity-map overhead on reads
Read-only queries paying for change tracking or session bookkeeping.
- **Fix:** use the ORM's read-only path (EF `AsNoTracking`, Hibernate read-only transactions/hints, SQLAlchemy column selects, Django `.values()`).

### 4. Cartesian explosion
Eager-loading two or more collections through JOINs multiplies rows (orders x items x payments).
- **Fix:** load collections in separate queries (split query, select-in loading, batch fetching) or project.

### 5. Work done in memory instead of the database
- **Detect:** filtering, sorting, counting, grouping, or paging after materializing a full list.
- **Fix:** push `WHERE`, `ORDER BY`, `LIMIT` and aggregates into the query, before materialization.

### 6. Pagination
- **Detect:** unbounded list queries; deep `OFFSET`; paging without a stable `ORDER BY`.
- **Fix:** always limit; keyset/cursor pagination (`WHERE id > :last ORDER BY id LIMIT n`) for deep pages; stable, indexed sort key.

### 7. Counting and existence
- **Detect:** `count() > 0`, loading a list just to check its length.
- **Fix:** an existence query (`AnyAsync`, `.exists()`, `existsBy...`, `findFirst` selecting only the id).

### 8. Batching writes
- **Detect:** load-modify-save loops; one INSERT/UPDATE per row; a transaction per row.
- **Fix:** bulk statements (`ExecuteUpdateAsync`, `createMany`/`updateMany`, `bulk_create`/`QuerySet.update()`, JPQL bulk `UPDATE` + JDBC batching, `CreateInBatches`). Warn that bulk statements usually bypass entity hooks, events, and interceptors.

### 9. Async and connection usage
- **Detect:** sync DB calls on async request paths (EF `ToList()` in async code, a sync session inside `async def`); long transactions holding connections; external HTTP calls inside a transaction.
- **Fix:** use the async API end to end, keep transactions short, and do network calls outside them.

### 10. Indexes and SQL inspection
- **Detect:** filters, joins, and sorts on columns with no index.
- **Fix:** suggest the index in the project's migration format, marked "verify against your schema". Always recommend logging the generated SQL and running `EXPLAIN` / `EXPLAIN ANALYZE` on the slow statement.

## Per-ORM quick map

| Problem | EF Core | Prisma / TypeORM / Drizzle | SQLAlchemy / Django | Hibernate / JPA | GORM |
|---|---|---|---|---|---|
| Eager load | `Include` / `ThenInclude` | `include` / `relations` / `with` | `selectinload`, `joinedload` / `select_related`, `prefetch_related` | `JOIN FETCH`, `@EntityGraph` | `Preload`, `Joins` |
| Projection | `.Select(x => new Dto(...))` | `select: {...}` / `.select([...])` / `db.select({...})` | `select(Model.a, Model.b)` / `.values()`, `.only()` | DTO / record / interface projection | `Select(...)` + `Scan(&dto)` |
| Read-only | `AsNoTracking()` | n/a (no change tracker) | column selects / `.values()` | `@Transactional(readOnly = true)`, read-only hint | n/a |
| Split collections | `AsSplitQuery()` | separate queries | `selectinload` / `prefetch_related` | `@BatchSize`, `hibernate.default_batch_fetch_size` | `Preload` (separate query) |
| Bulk write | `ExecuteUpdateAsync` / `ExecuteDeleteAsync` | `updateMany` / `.update().where()` | `update()` statement / `QuerySet.update()`, `bulk_create` | JPQL `UPDATE`, JDBC batching | `Model(...).Where(...).Updates(...)`, `CreateInBatches` |
| Show SQL | `ToQueryString()`, `LogTo` | `log: ['query']` / `logging: true` / `logger: true` | `echo=True` / `str(qs.query)` | `spring.jpa.show-sql`, `hibernate.generate_statistics` | `db.Debug()`, `db.ToSQL(...)` |

## Output format

```
## Summary
<stack + ORM detected; one paragraph: what was slow and the headline fixes>

## Findings
1. [N+1] <file:line> - <problem> -> <fix>
2. [No projection] ... -> ...

## Optimized code
<the rewritten query/method, ready to paste>

## Verify on your side
- <index suggestions, SQL/EXPLAIN to run, anything needing the real schema or data>
```

## Principles

- Never change query results silently. If a fix alters behavior, say so.
- Prefer projections over eager loading when the caller only reads data.
- Measure, don't guess: look at the generated SQL and the query plan.
- Optimize the query that's actually slow, not every micro-pattern.
- Don't drop to raw SQL unless the ORM genuinely can't express the efficient query - and when you do, keep it parameterized.
