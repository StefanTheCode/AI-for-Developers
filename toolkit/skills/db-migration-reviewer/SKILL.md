---
name: db-migration-reviewer
description: Review database schema migrations for destructive or risky operations before they hit production - dropped columns/tables, non-nullable adds without defaults, renames that drop data, type changes, and long-locking operations. Works with EF Core migrations, Prisma Migrate, TypeORM/Drizzle/Knex migrations, Alembic and Django migrations, Flyway and Liquibase, golang-migrate and goose, and plain SQL migration files. Use this skill whenever the user has a migration file, a pending schema change, a generated Migration.cs / migration.sql / Alembic revision / V__*.sql changelog, or asks "is this migration safe / will this migration lose data / review my migration / zero-downtime migration". Always use this skill for migration-safety questions; it runs a fixed risk checklist.
category: Database
version: 1.0.0
---

# Database Migration Reviewer

Catch data-loss and downtime risks in a schema migration before it runs in production, and suggest a safer rewrite (often an expand/contract split). The risks are the same in every tool; only the syntax differs.

## Input - point it at your migrations
Works on a target: a **migration file**, a **migrations folder**, a **project/repo**, or a **GitHub URL**. Review the most recent unapplied migration(s) first. Always read both directions (up/down) when the tool has them, and look at the **SQL that will actually run**, not just the ORM DSL.

## Step 1 - detect the migration tool

| Manifest / signal | Tool | Where migrations live | See the real SQL with |
|---|---|---|---|
| `*.csproj` + `Microsoft.EntityFrameworkCore.Design` | EF Core | `Migrations/*.cs` (skip `*.Designer.cs`, `*ModelSnapshot.cs`) | `dotnet ef migrations script <from> <to>` |
| `package.json` + `prisma` | Prisma Migrate | `prisma/migrations/*/migration.sql` | the `migration.sql` itself; `prisma migrate diff` |
| `package.json` + `typeorm` / `drizzle-kit` / `knex` | TypeORM / Drizzle / Knex | `src/migrations`, `drizzle/*.sql`, `migrations/*.js` | generated SQL / `up()` body |
| `alembic.ini`, `sqlalchemy` | Alembic | `alembic/versions/*.py` | `alembic upgrade <rev> --sql` (offline mode) |
| `django` in deps | Django | `<app>/migrations/*.py` | `python manage.py sqlmigrate <app> <name>` |
| `pom.xml`/`build.gradle` + `flyway` | Flyway | `db/migration/V*__*.sql` | the SQL file |
| `pom.xml`/`build.gradle` + `liquibase` | Liquibase | `db/changelog/*.xml/yaml/sql` | `liquibase update-sql` |
| `go.mod` + `golang-migrate` / `goose` | golang-migrate / goose | `*.up.sql` + `*.down.sql` / `-- +goose Up/Down` | the SQL file |

```
# find candidate migration files
find <target> \( -path "*Migrations*" -name "*.cs" ! -name "*.Designer.cs" ! -name "*Snapshot.cs" \) \
  -o -path "*prisma/migrations*" -name "migration.sql" -o -path "*alembic/versions*" -name "*.py" \
  -o -path "*/migrations/*" -name "*.py" -o -name "V*__*.sql" -o -name "*.up.sql" | grep -v node_modules
```

Also establish the **database engine** (locking behavior differs a lot) and, if the user knows, **table sizes**.

## Step 2 - risk checklist (universal)
1. 🔴 **Dropped column/table** - permanent data loss. Confirm it's intended, backed up, and no running code still reads it.
2. 🔴 **Add non-nullable column without default** to a populated table - fails, or forces a bad default. Add nullable, backfill, then set `NOT NULL`; or add with a default.
3. 🔴 **Rename modeled as drop + add** - autogenerators often can't detect renames (EF Core, Prisma, Alembic autogenerate, Django without the rename prompt) = data loss. Use an explicit rename.
4. 🔴 **Rename/drop in the same deploy as the code change** - old app instances still running during a rolling deploy will break. Expand/contract instead.
5. 🟡 **Type change** (widening/narrowing, `varchar` to `int`, precision change) - can truncate, fail on existing rows, or force a full table rewrite.
6. 🟡 **Long-locking operation on a big table** - index build, `NOT NULL`, FK or CHECK constraint validation, column type rewrite. Use online/concurrent variants (see engine notes).
7. 🟡 **Data migration mixed with schema change** - a big `UPDATE` inside the migration transaction holds locks for its whole run. Batch it, or run it as a separate job.
8. 🟡 **No down / irreversible down** - migrations should be reversible where feasible; if not, say so explicitly.
9. 🟡 **Edited an already-applied migration** - checksums break (Flyway/Liquibase) or environments drift (all tools). Add a new migration instead.
10. 🟢 **Multiple unrelated changes** in one migration - harder to roll back; suggest splitting.

## Engine notes (locking)
- **PostgreSQL:** `CREATE INDEX CONCURRENTLY` (cannot run inside a transaction); add FKs/CHECKs as `NOT VALID` then `VALIDATE CONSTRAINT`; set `lock_timeout` so a blocked DDL doesn't queue every query behind it. Adding a column with a constant default is metadata-only on 11+.
- **SQL Server:** `CREATE INDEX ... WITH (ONLINE = ON)` (edition-dependent); adding a NOT NULL column with a default is metadata-only on Enterprise 2012+, otherwise a size-of-data operation.
- **MySQL / MariaDB:** check `ALGORITHM=INPLACE|INSTANT, LOCK=NONE` support for the specific change; for big tables consider gh-ost or pt-online-schema-change.
- **SQLite:** most `ALTER` changes mean a table rebuild - fine for small DBs, plan for it otherwise.

## Per-tool notes

### C# / .NET (EF Core)
Read `Up()` and `Down()`. Watch for `DropColumn`/`DropTable`, `AddColumn(nullable: false)` without `defaultValue`, and drop+add pairs that should be `RenameColumn`/`RenameTable`. For concurrent/online builds use `migrationBuilder.Sql(...)` with `suppressTransaction: true`. Generate an idempotent script for production (`dotnet ef migrations script --idempotent`) rather than `Database.Migrate()` at app startup on multi-instance deployments.

### TypeScript / Node
Prisma: always read `migration.sql`; Prisma warns about data loss in dev but the SQL is the source of truth - edit it (e.g. `CREATE INDEX CONCURRENTLY`, rename instead of drop+add) before `migrate deploy`. TypeORM/Knex: check `up()`/`down()` pairs; disable the per-migration transaction where the engine requires it for concurrent DDL. Drizzle: review the generated `.sql`, especially rename prompts.

### Python
Alembic: autogenerate never detects renames - check for `drop_column` + `add_column` pairs; for concurrent indexes use `op.create_index(..., postgresql_concurrently=True)` inside `with op.get_context().autocommit_block():`. Django: check `RemoveField`/`DeleteModel`, `AlterField` to non-null, and `RunPython` without `reverse_code`; set `atomic = False` for `AddIndexConcurrently`; use `SeparateDatabaseAndState` for staged removals.

### Java / Kotlin (Flyway / Liquibase)
Never edit an applied `V__` script; add a new one. Liquibase: check every changeSet has a `rollback` (or is auto-rollbackable) and `runInTransaction="false"` where concurrent DDL needs it. Make sure `spring.jpa.hibernate.ddl-auto` is `validate` or `none` in production, not `update`.

### Go (golang-migrate / goose)
Every `.up.sql` needs a matching `.down.sql` (goose: `-- +goose Down`). For concurrent index builds in goose use `-- +goose NO TRANSACTION`; golang-migrate runs files as given, so keep concurrent DDL in its own file.

## Safer pattern: expand / contract
For risky renames/type changes on live systems, split across deploys:
1. **Expand** - add the new column/table (nullable), backfill in batches, dual-write from the app.
2. **Migrate** - switch reads to the new column; verify.
3. **Contract** - drop the old column in a later migration once nothing uses it.

## Output
```
## Verdict: safe / risky / data-loss
<tool + engine detected; files reviewed>

## Findings
🔴 <op> at <file:line> - <risk> -> <safer approach>
🟡 ...

## Suggested rewrite
<migration code in the same tool's format, or the expand/contract plan>

## Before you run
<backup / backfill / lock_timeout / off-peak / table-size notes>
```

## Principles
- A migration that loses data should be a deliberate, backed-up decision - never a surprise.
- On a live DB, schema changes are deploys with timing and locking implications, not just code.
- Review the SQL that will run, not only the ORM DSL that produced it.
- Reversibility matters: prefer changes you can roll back.

## How to use it & best prompts
"Review this migration before I deploy", "will this Prisma migration lose data", "make this column rename safe on a live table", "split this risky Alembic revision into expand/contract", "is this Flyway script safe on a 50M-row table". Pairs with `db-index-advisor`.
