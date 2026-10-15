# How to use - Database Performance Auditor (agent)

**What it is.** An agent that audits a whole data layer in any stack - **EF Core, Dapper, Prisma, TypeORM, Drizzle, SQLAlchemy, Django ORM, Hibernate/JPA, Spring Data, GORM**, or raw SQL - covering query patterns, indexing, ORM and pool configuration, resiliency, and bulk-operation opportunities, and produces a ranked report with fixes in your ORM's own idiom.

**When to reach for it.** "Why is my data layer slow", a pre-release DB performance pass, or a second opinion on ORM usage across a repo.

**How to use it.** Point it at a repo (path or GitHub URL); it detects the stack and explores the data layer itself. Example prompts:
- "Audit this repo's database performance."
- "Pre-release EF Core performance review of this solution."
- "Our Prisma API gets slow under load - find the worst queries."
- "Check this Django project for N+1s and missing indexes."
- "Audit Hibernate usage in this Spring Boot service."

**Get the best out of it.** Give it the whole codebase so it sees config and queries together. Act on the ranked order. For deep single-query rewrites use `orm-query-optimizer`; for indexes use `db-index-advisor`; for configuration use `orm-config-auditor`; for retries and timeouts use `db-resiliency-setup`.

**Won't do.** It can't read your live query plan - index and plan claims come marked "verify" with the command to check them.
