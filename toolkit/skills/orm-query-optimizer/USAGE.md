# How to use - ORM Query Optimizer

**What it is.** A skill that reviews your ORM/data-access code and rewrites it for performance. It detects the stack first (EF Core, Prisma, TypeORM, Drizzle, SQLAlchemy, Django ORM, Hibernate/JPA, GORM), runs a fixed 10-point checklist (N+1, over-fetching, tracking overhead, cartesian explosion, in-memory filtering, pagination, existence checks, batching, async/connection usage, indexes), and gives you back optimized code plus a one-line reason for every change.

**When to reach for it.** A slow endpoint, a query you suspect is doing too much, a code review where database performance matters, or any time you say N+1 / eager loading / "why is this query slow".

**How to use it.** Point it at your code - no pasting needed. Give it a **file path**, a **folder**, a whole **project/repo**, or a **GitHub URL** (it clones and scans). Example prompts:
- "This EF query is slow, optimize it."
- "This Prisma query is slow - check `src/orders/`."
- "Find N+1 queries in my Django views."
- "Review the JPA repositories in this Spring Boot app for over-fetching."
- "Scan this Go service's GORM usage: https://github.com/me/myapi"

**How to get the best out of it.**
- **Give it the models/entities and relationships**, not just the query. Half the wins (projections, split queries, batch loading) depend on the data shape.
- **Tell it the symptom** ("slow on lists of 10k+", "times out on the dashboard"). It tunes the fix to the real problem.
- **Ask it to show the generated SQL** and the `EXPLAIN` to run when you want to verify a fix rather than trust it.
- For deeper cases it loads the matching reference: `ef-core-patterns.md`, `prisma-typeorm-patterns.md`, `sqlalchemy-django-patterns.md`, `hibernate-jpa-patterns.md`, or `gorm-patterns.md`.

**What it won't do.** It can't see your real database, so index suggestions come marked "verify" - confirm them against your schema and query plan. It won't change query results silently; if a fix alters behavior it tells you.
