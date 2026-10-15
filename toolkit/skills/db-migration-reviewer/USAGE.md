# How to use - Database Migration Reviewer

**What it is.** Reviews schema migrations for data-loss and downtime risks (drops, non-nullable adds, drop+add renames, type changes, long locks, data updates inside the migration) and suggests a safer rewrite - often expand/contract. It detects your tool first: EF Core, Prisma Migrate, TypeORM/Drizzle/Knex, Alembic, Django, Flyway, Liquibase, golang-migrate, or goose - and reviews the SQL that will actually run.

**When to reach for it.** Before applying any migration to a populated or production database.

**How to use it.** Point it at a migration file, the migrations folder, or the project (path or GitHub URL). Example prompts:
- "Review this EF Core migration before I deploy it."
- "Will this Prisma migration lose data?"
- "Make this column rename safe on a live Postgres table - it's an Alembic revision."
- "Check my Flyway scripts for long-locking operations."
- "Is this Django migration reversible?"

**What you get.** A verdict (safe / risky / data-loss), ranked findings with file and line, a rewritten migration in the same tool's format or an expand/contract plan, and a "before you run" list (backup, backfill, lock timeout, timing).

**Get the best out of it.** Run it on the newest unapplied migration. Mention the database engine and table sizes - they change the locking advice. Ask for the expand/contract split when the change is risky on a live system. Pairs with `db-index-advisor` for new indexes.

**Won't do.** It can't see your data, so backfill and cast-safety items come marked "verify".
