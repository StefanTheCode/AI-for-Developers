# How to use - SQL - ORM Converter

**What it is.** Converts raw SQL to ORM query code and ORM code to the SQL it generates, both directions, preserving the result set and flagging constructs that won't translate. It detects the target first: EF Core LINQ, Prisma, Drizzle, TypeORM QueryBuilder, Knex, SQLAlchemy 2.0, Django QuerySet, JPQL / Criteria / Spring Data, jOOQ, or GORM - and can port a query between two ORMs.

**When to reach for it.** Migrating raw SQL, stored procedures, or driver-level queries (ADO.NET, JDBC, pg, psycopg) to an ORM; checking what SQL your ORM code produces; moving a query between stacks.

**How to use it.** Paste the query, or point it at a project (path or GitHub URL) for the model context. Example prompts:
- "Convert this SQL to EF Core LINQ."
- "Write this report query with Drizzle."
- "What SQL does this Django QuerySet generate?"
- "Port this stored procedure to SQLAlchemy."
- "Turn this native query into JPQL with a DTO projection."
- "Rewrite this raw `database/sql` query with GORM."

**What you get.** The converted query ready to paste, the expected generated SQL (or the command to print it), and semantics notes on anything that differs (NULLs, join type, empty aggregates, collation) or falls back to raw SQL.

**Get the best out of it.** Give it the model/entity definitions for SQL -> ORM. Mention the database dialect. Ask to see the generated SQL to verify. Pairs with `orm-query-optimizer`.

**Won't do.** It won't force awkward ORM code for queries that genuinely belong in SQL (window functions, recursive CTEs, heavy analytics) - it flags those and uses the ORM's parameterized raw SQL API instead.
