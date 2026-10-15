---
name: temporal-tables-setup
description: Set up row history and auditing in any stack - database-native system-versioned temporal tables (SQL Server, MariaDB), PostgreSQL history via the temporal_tables extension or a trigger-based history table, or an application-level audit trail through ORM hooks (EF Core SaveChangesInterceptor, Prisma client extensions, TypeORM subscribers, SQLAlchemy session events, django-simple-history, Hibernate Envers, GORM hooks). Use this skill whenever the user wants temporal tables, system versioning, row history, audit trail, audit log, point-in-time / AS OF queries, who-changed-what, change tracking for compliance, or asks "how do I track changes / keep history of records / audit my entities". Always use this skill for auditing/history requests; it picks database-native history or an ORM-hook audit log based on the database and requirements.
category: Database
version: 1.0.0
---

# Temporal Tables / Auditing Setup

Add change history to tables - either **database-native history** (automatic, queryable point-in-time state) or an **application-level audit log** (DB-agnostic, captures who and why) - depending on the database and what the user actually needs.

## Input - point it at your project
Works on a target: a **file**, **folder**, **project/repo**, or **GitHub URL**. Report the database and ORM you detected and which tables you'd version.

## Step 1 - detect the stack and the database

| Manifest | ORM / migrations | Detect the database from |
|---|---|---|
| `*.csproj` | EF Core | `UseSqlServer` / `UseNpgsql` / `UseMySql` |
| `package.json` | Prisma / TypeORM / Drizzle / Knex | `provider = "..."` in `schema.prisma`, DataSource `type`, driver package |
| `pyproject.toml`, `requirements*.txt` | SQLAlchemy + Alembic / Django | URL scheme (`postgresql+psycopg`, `mssql+pyodbc`, `mariadb+...`), `ENGINE` |
| `pom.xml`, `build.gradle(.kts)` | JPA / Hibernate + Flyway/Liquibase | `spring.datasource.url` (`jdbc:sqlserver`, `jdbc:postgresql`, `jdbc:mariadb`) |
| `go.mod` | GORM / golang-migrate / goose | driver import (`gorm.io/driver/postgres`, `go-mssqldb`, `go-sql-driver/mysql`) |

## Step 2 - decide what is needed
- **Point-in-time state** ("what did this order look like last Tuesday") - database-native history.
- **Who changed what and why** (actor, request, reason) - application audit log; native history does not record the actor.
- **Both** - native history for state + a light audit log (or an actor column on the versioned table).

## Database-native history

| Database | Native support | How |
|---|---|---|
| SQL Server 2016+ / Azure SQL | system-versioned temporal tables | `PERIOD FOR SYSTEM_TIME` + `SYSTEM_VERSIONING = ON`; query `FOR SYSTEM_TIME AS OF / BETWEEN / ALL` |
| MariaDB 10.3+ | system-versioned tables | `ALTER TABLE t ADD SYSTEM VERSIONING`; query `FOR SYSTEM_TIME AS OF TIMESTAMP '...'` |
| PostgreSQL | none built in | `temporal_tables` extension (if your host allows it) or a trigger-based history table (works everywhere) |
| MySQL | none | trigger-based history table or app audit log |

### SQL Server (raw SQL, any stack)
```sql
ALTER TABLE dbo.Orders ADD
  ValidFrom datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL DEFAULT SYSUTCDATETIME(),
  ValidTo   datetime2 GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL DEFAULT CONVERT(datetime2, '9999-12-31 23:59:59.9999999'),
  PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);
ALTER TABLE dbo.Orders SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrdersHistory));

SELECT * FROM dbo.Orders FOR SYSTEM_TIME AS OF '2026-10-01T00:00:00' WHERE Id = @id;
```
Retention: `HISTORY_RETENTION_PERIOD` (Azure SQL / SQL Server 2017+). Schema changes to a versioned table need care - some require turning versioning off and on in the migration.

### PostgreSQL - trigger-based history (portable)
```sql
ALTER TABLE orders ADD COLUMN sys_period tstzrange NOT NULL DEFAULT tstzrange(now(), NULL);
CREATE TABLE orders_history (LIKE orders);

CREATE OR REPLACE FUNCTION orders_versioning() RETURNS trigger AS $$
BEGIN
  INSERT INTO orders_history SELECT (OLD).*;           -- history has identical columns (LIKE orders)
  UPDATE orders_history SET sys_period = tstzrange(lower(OLD.sys_period), now())
   WHERE id = OLD.id AND sys_period = OLD.sys_period;  -- close the copied row's period
  IF TG_OP = 'UPDATE' THEN NEW.sys_period := tstzrange(now(), NULL); RETURN NEW; END IF;
  RETURN OLD;
END $$ LANGUAGE plpgsql;

CREATE TRIGGER orders_versioning BEFORE UPDATE OR DELETE ON orders
  FOR EACH ROW EXECUTE FUNCTION orders_versioning();
```
Point-in-time: `SELECT * FROM orders WHERE sys_period @> $1::timestamptz UNION ALL SELECT * FROM orders_history WHERE sys_period @> $1::timestamptz`. Capture the actor by having the app run `SET LOCAL app.user_id = '...'` per transaction and storing `current_setting('app.user_id', true)` in a history column. With the `temporal_tables` extension, the trigger is `versioning('sys_period', 'orders_history', true)`; check your managed host supports it before choosing it. Ship the trigger through the project's migration tool.

## Per-stack wiring

### C# / .NET (EF Core)
SQL Server temporal tables are first-class (EF Core 6+):
```csharp
modelBuilder.Entity<Order>().ToTable("Orders", t => t.IsTemporal(tt =>
{
    tt.HasPeriodStart("ValidFrom");
    tt.HasPeriodEnd("ValidTo");
    tt.UseHistoryTable("OrdersHistory");
}));

var asOf = await db.Orders.TemporalAsOf(timestamp).ToListAsync(ct);
var changes = await db.Orders.TemporalAll().Where(o => o.Id == id)
    .OrderBy(o => EF.Property<DateTime>(o, "ValidFrom")).ToListAsync(ct);
```
Also `TemporalFromTo`, `TemporalBetween`, `TemporalContainedIn`. Application audit log (any provider, captures "who") via a `SaveChangesInterceptor`:
```csharp
public class AuditInterceptor(ICurrentUser user) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData e, InterceptionResult<int> r, CancellationToken ct = default)
    {
        var ctx = e.Context!;
        foreach (var entry in ctx.ChangeTracker.Entries()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                     .ToList())
        {
            ctx.Add(new AuditEntry {
                Entity = entry.Metadata.GetTableName(), KeyValues = /* pk */,
                Action = entry.State.ToString(), Changes = /* changed props: OriginalValue -> CurrentValue */,
                User = user.Id, At = DateTime.UtcNow });
        }
        return base.SavingChangesAsync(e, r, ct);
    }
}
```
Materialize the entries (`ToList()`) before adding audit rows, and skip `AuditEntry` itself. Generated keys of `Added` entities are only known after save - capture them in `SavedChangesAsync` if needed.

### TypeScript / Node
- Native history: write the SQL above in a Prisma/Drizzle/TypeORM/Knex migration (Prisma: create with `--create-only` and edit `migration.sql`); map the history table as a read-only model.
- Audit log: Prisma client extension (`prisma.$extends({ query: { $allModels: { async update({ model, args, query }) { ... } } } })`); the older `$use` middleware is deprecated. TypeORM: an `EntitySubscriberInterface` with `afterInsert`/`afterUpdate`/`afterRemove` (use `event.databaseEntity` for old values). Pass the actor via `AsyncLocalStorage`.

### Python
- SQLAlchemy: a `before_flush` session event iterating `session.new`, `session.dirty`, `session.deleted` and using `inspect(obj).attrs[x].history` for old/new values; or SQLAlchemy-Continuum for versioned tables. Ship native history SQL through Alembic `op.execute(...)`.
- Django: `django-simple-history` (`history = HistoricalRecords()` on the model, records `history_user` from the request via its middleware) or `django-auditlog`. Signals (`pre_save`/`post_save`) work but miss `QuerySet.update()` and bulk operations.

### Java / Kotlin (Spring)
- Hibernate Envers: `@Audited` on entities + a custom `RevisionEntity` with a `RevisionListener` that stores the current user; query with `AuditReader` (`forRevisionsOfEntity`, `forEntitiesAtRevision`). Spring Data Envers adds `RevisionRepository`.
- Spring Data JPA auditing (`@CreatedBy`, `@LastModifiedBy`, `@LastModifiedDate` + `AuditorAware`) records who touched the current row only - not history.
- Native history SQL through Flyway/Liquibase; map the history table as a read-only `@Immutable` entity.

### Go (GORM)
- Model hooks `BeforeUpdate`/`AfterUpdate`/`AfterDelete`, or global `db.Callback().Update().After("gorm:update").Register(...)` callbacks writing audit rows; pass the actor via `context.Context` (`db.WithContext(ctx)`, read with `tx.Statement.Context`).
- Hooks don't fire for raw `Exec` or some batch updates - native history (triggers / temporal tables) is safer when completeness matters.

## Output
```
## Summary
<database + ORM detected; point-in-time vs who-changed-what; chosen approach and why>

## Setup
<migration SQL / ORM configuration / hook code in the project's idiom>

## Querying history
<as-of and full-history queries for the main entity>

## Retention and PII
<retention plan; which fields are excluded or masked>
```

## Choosing
- **Native history (temporal / system-versioned / triggers):** automatic, complete even for raw SQL and bulk updates, exact point-in-time; doesn't record *who* unless you add it.
- **ORM-hook audit log:** any database, records who/why, but only sees writes that go through the ORM (bulk/raw statements bypass it) and you maintain it.
- Combine: native history for state + a light audit log for actor/reason.

## Principles
- Decide what you need: **point-in-time state** vs **who-changed-what**.
- Completeness matters for compliance - triggers and native versioning catch writes that bypass the ORM.
- Don't log sensitive field *values* into audit rows without considering PII.
- History tables grow - plan retention/cleanup and index the period columns.

## How to use it & best prompts
"Add history to my Order entity", "track who changed what", "set up SQL Server temporal tables with EF Core", "point-in-time query for this record", "add system versioning in MariaDB", "audit trail for Postgres without extensions", "set up Hibernate Envers". (Stefan has a blog post on EF Core temporal tables - this complements it.) Pairs with `db-migration-reviewer` and `crud-vs-event-sourcing`.
