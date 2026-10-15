# How to use - ORM Configuration Auditor

**What it is.** Audits your ORM and data-layer configuration for the settings that quietly cause performance problems, data leaks, or odd behavior - SQL/parameter logging, schema auto-sync in production, context/session lifetime, lazy loading, tracking defaults, pooling, mapping conventions, and cascade/delete behavior. It detects the stack first: EF Core, Prisma, TypeORM, Drizzle, SQLAlchemy, Django ORM, Hibernate/JPA (Spring), or GORM.

**When to reach for it.** Reviewing data-layer setup before production, or when the ORM "behaves oddly" (extra queries, surprising cascades, connection exhaustion).

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Review my DbContext and entity configuration."
- "Is my Prisma client setup production-ready?"
- "Why is Hibernate issuing extra queries? Check my application.yml and entities."
- "Audit my SQLAlchemy engine and session setup."
- "Is `synchronize: true` in my TypeORM config a problem?"

**What you get.** Ranked findings with file and line, and a corrected configuration in your project's idiom plus a sample per-entity mapping.

**Get the best out of it.** Include the registration/bootstrap code and the mapping files (`OnModelCreating`, `schema.prisma`, entities/models, `application.yml`). Pairs with `orm-query-optimizer` (query-level) and `db-resiliency-setup` (retry, timeouts, pools).

**Won't do.** It reviews configuration, not individual query bodies - use `orm-query-optimizer` for those.
