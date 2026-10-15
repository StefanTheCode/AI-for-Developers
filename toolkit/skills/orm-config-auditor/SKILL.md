---
name: orm-config-auditor
description: Audit ORM and data-layer configuration for correctness and performance pitfalls in any stack - EF Core DbContext/OnModelCreating, Prisma schema and client setup, TypeORM DataSource, Drizzle, SQLAlchemy engine/session, Django DATABASES and models, Hibernate/JPA and Spring Data settings, GORM config. Checks tracking defaults, lazy loading, SQL/parameter logging that leaks data, pooling, connection lifetime, schema auto-sync in production, missing mapping configuration (lengths, precision, nullability, cascade/delete behavior), and context/session lifetime. Use this skill whenever the user shares a DbContext, AddDbContext setup, schema.prisma, DataSource/ormconfig, create_engine/sessionmaker, settings.py DATABASES, application.yml spring.jpa/datasource, or gorm.Open config, or asks "review my ORM setup / is my EF configuration right / why is Hibernate behaving oddly". Always use this skill for ORM configuration reviews; it runs a fixed checklist.
category: Database
version: 1.0.0
---

# ORM Configuration Auditor

Review how the ORM and its connection/session are set up - the settings that quietly cause performance problems, data leaks, or surprising behavior. This is configuration, not individual query bodies (use `orm-query-optimizer` for those).

## Input - point it at your project
Works on a target: a **file**, **folder**, **project/repo**, or **GitHub URL**. Report which config files you read.

## Step 1 - detect the stack and where config lives

| Manifest | ORM | Read these |
|---|---|---|
| `*.csproj` | EF Core | `AddDbContext`/`AddDbContextPool` registration, `DbContext` class, `OnModelCreating`, `IEntityTypeConfiguration<T>` classes, `appsettings*.json` |
| `package.json` | Prisma / TypeORM / Drizzle / Sequelize | `schema.prisma` + `new PrismaClient(...)`; `DataSource`/`ormconfig`; `drizzle.config.ts` + `drizzle(...)` |
| `pyproject.toml`, `requirements*.txt` | SQLAlchemy / Django ORM | `create_engine`, `sessionmaker`, model `relationship(...)`; `settings.py` `DATABASES`, model `Meta`, `on_delete` |
| `pom.xml`, `build.gradle(.kts)` | Hibernate / JPA / Spring Data | `application.yml/properties` (`spring.jpa.*`, `spring.datasource.*`), `@Entity` mappings |
| `go.mod` | GORM / sqlx / ent | `gorm.Open(..., &gorm.Config{...})`, `sql.DB` pool settings, model tags |

```
grep -rln "DbContext\|OnModelCreating\|IEntityTypeConfiguration\|AddDbContext" --include=*.cs <target>
grep -rln "new PrismaClient\|new DataSource\|createConnection\|drizzle(" <target>/src
grep -rn "create_engine\|sessionmaker\|DATABASES\s*=" --include=*.py <target>
grep -rn "spring.jpa\|spring.datasource\|hibernate\." <target>/src/main/resources
grep -rn "gorm.Open\|gorm.Config\|SetMaxOpenConns" --include=*.go <target>
```

## Universal checklist
1. 🔴 **Parameter values / SQL logged unconditionally** - leaks PII and secrets into logs. Gate verbose SQL logging behind development config.
2. 🔴 **Schema auto-sync in production** - the ORM altering the live schema on startup (TypeORM `synchronize: true`, Hibernate `ddl-auto=update/create`, GORM `AutoMigrate` on boot, EF `EnsureCreated`). Use reviewed migrations.
3. 🔴 **Wrong context/session lifetime** - one shared session/context across requests or threads (not thread-safe, unbounded identity map). Scope it to the request/unit of work.
4. 🟡 **Lazy loading on by default** - silent N+1. Prefer explicit eager loading or projection.
5. 🟡 **Change tracking default for a read-heavy app** - consider no-tracking/read-only by default and opt in for writes.
6. 🟡 **Pool not configured / connection lifetime unset** - defaults that don't match instance count or proxies that drop idle connections (see `db-resiliency-setup`).
7. 🟡 **Missing or over-broad mapping conventions** - unbounded string lengths, decimal/money precision, required/optional, enum storage, timezone-aware timestamps.
8. 🟡 **Cascade/delete behavior inferred, not chosen** - surprise cascades or orphaned rows. Make it explicit per relationship.
9. 🟡 **Giant single config block** - split mapping per entity/model once it grows.
10. 🟢 **No transient-fault retry or timeouts** - point to `db-resiliency-setup`.

## Per-stack checks

### C# / .NET (EF Core)
- 🔴 `EnableSensitiveDataLogging()` / `EnableDetailedErrors()` unconditional - gate behind `IsDevelopment()`.
- 🟡 `UseLazyLoadingProxies()` - prefer explicit `Include` / projection.
- 🟡 Read-heavy context - `UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)` as default, opt into tracking for writes.
- 🟡 High-traffic API without pooling - `AddDbContextPool` (or `AddPooledDbContextFactory`) cuts allocation cost; keep no per-request state in the context.
- 🟡 Large `OnModelCreating` - move to `IEntityTypeConfiguration<T>` + `ApplyConfigurationsFromAssembly`.
- 🟡 Conventions - `HasMaxLength`, `HasPrecision` (or `ConfigureConventions` for global defaults), `IsRequired`, `OnDelete(DeleteBehavior.Restrict/Cascade)` chosen deliberately.
- 🟢 `EnableRetryOnFailure` not set (see `db-resiliency-setup`).
- 🟢 DbContext lifetime - scoped is correct; singleton/transient registrations cause bugs. Use `IDbContextFactory` for background services and Blazor Server.

### TypeScript / Node
- Prisma: one `PrismaClient` per process (a new client per request exhausts connections; in dev hot-reload cache it on `globalThis`); `log: ['query']` only in dev; `connection_limit` set for serverless; `onDelete`/`onUpdate` explicit on relations; `@db.VarChar(n)` / `@db.Decimal(p, s)` where size matters.
- TypeORM: `synchronize: false` and `migrationsRun` deliberate in prod; `logging` limited; `eager: true` / `lazy: true` relations reviewed (eager on many relations = huge joins); `cascade` explicit; `precision`/`scale`/`length` on columns.
- Drizzle: `logger` off in prod; pool created once and reused.

### Python
- SQLAlchemy: `echo=False` in prod; one `Engine` per process; `sessionmaker` scoped to request/unit of work (not module-global sessions); `expire_on_commit` chosen deliberately (async sessions usually need `False`); relationship `lazy=` defaults reviewed (`"raise"` catches accidental lazy loads); `pool_pre_ping`/`pool_recycle` set.
- Django: `DEBUG = False` (DEBUG keeps every SQL query in memory); `CONN_MAX_AGE` set; `on_delete` deliberate on every FK; `DecimalField(max_digits, decimal_places)` for money; `USE_TZ = True`.

### Java / Kotlin (Hibernate / Spring)
- `spring.jpa.hibernate.ddl-auto` = `validate` or `none` in prod.
- `spring.jpa.open-in-view=false` - the default `true` keeps a session open for the whole request and hides lazy-loading N+1 (Spring Boot logs a warning about it).
- `@ManyToOne`/`@OneToOne` default to `FetchType.EAGER` in JPA - switch to `LAZY` and fetch explicitly.
- `spring.jpa.show-sql` / `org.hibernate.orm.jdbc.bind` (Hibernate 6) parameter logging off in prod.
- `hibernate.default_batch_fetch_size` and JDBC batching (`hibernate.jdbc.batch_size`, `order_inserts`) configured for write-heavy apps.
- `CascadeType.ALL` / `orphanRemoval` only where the child is truly owned.

### Go (GORM)
- `Logger: logger.Default.LogMode(logger.Silent/Warn)` in prod (Info logs every statement).
- `AutoMigrate` not run on every boot in prod; use golang-migrate/goose/Atlas.
- `PrepareStmt: true` for hot repeated queries; `SkipDefaultTransaction: true` when single writes don't need the implicit transaction.
- Underlying `sql.DB` pool settings applied (`db.DB()` then `SetMaxOpenConns`, `SetConnMaxLifetime`).
- `constraint:OnDelete:` tags explicit on associations.

## Output
```
## Summary
<stack + ORM detected; config files reviewed>

## Findings (ranked)
🔴 [Sensitive logging] <file:line> - <risk> -> <fix>
🟡 ...

## Suggested configuration
<corrected setup in the project's idiom + one sample per-entity mapping>
```

## Principles
- Defaults you didn't set are still decisions - make tracking, lazy loading, logging, and schema sync explicit.
- Entity/model mapping belongs in per-entity units once the central config grows.
- Never leak parameter values into logs in production.
- Cascade/delete behavior should be intentional, not whatever the ORM inferred.
- The ORM never changes the production schema on its own - migrations do.

## How to use it & best prompts
"Review my DbContext setup", "is my Prisma/TypeORM config production-ready", "why is Hibernate doing extra queries", "audit my SQLAlchemy engine and session setup", "split OnModelCreating into per-entity config". Pairs with `orm-query-optimizer` and `db-resiliency-setup`.
