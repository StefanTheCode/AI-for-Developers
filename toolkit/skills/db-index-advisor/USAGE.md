# How to use - Database Index Advisor

**What it is.** Recommends indexes based on how your tables are actually queried (filters, joins, ordering, uniqueness) and generates the definition with correct column order in your project's own format - EF Core `HasIndex`, Prisma `@@index`, TypeORM/Drizzle, SQLAlchemy/Django, JPA + Flyway/Liquibase, GORM tags, or plain SQL - without over-indexing. Knows the differences between PostgreSQL, SQL Server, MySQL/MariaDB, and SQLite (covering, partial, online builds).

**When to reach for it.** A slow query you suspect needs an index, or designing indexes for a new model.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "What indexes does this EF Core project need?"
- "Add the right `@@index` entries to my Prisma schema for these queries."
- "Design a composite index for filtering by customer + status, sorting by date."
- "Which indexes is my Django app missing on the orders table?"
- "Why is this Spring Data `findBy` slow - am I missing an index?"

**What you get.** A ranked list of indexes, each tied to the query it serves and the reason for its column order; paste-ready definitions; a list of redundant index candidates; and the `EXPLAIN` / execution plan steps to confirm.

**Get the best out of it.** Let it scan the query sites so recommendations match real usage. Tell it which tables are large or write-heavy. Ask it to explain column order. Pairs with `orm-query-optimizer` (query-level fixes) and `db-migration-reviewer` (shipping the index safely on a live table).

**Won't do.** It can't read your live query plan or index usage stats, so it marks recommendations to verify against `EXPLAIN` / the execution plan.
