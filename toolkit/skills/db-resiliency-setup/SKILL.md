---
name: db-resiliency-setup
description: Add database resiliency to any stack - retry on transient failures with backoff, command/statement timeouts, connection pool sizing, and safe retry around transactions. Covers EF Core execution strategy (EnableRetryOnFailure) and Polly for .NET, cockatiel / p-retry plus pg/Prisma pool settings for Node, tenacity plus SQLAlchemy pool options for Python, Resilience4j / Spring Retry plus HikariCP for Java, and database/sql / pgxpool settings plus backoff libraries for Go. Use this skill whenever the user wants connection resiliency, retry on failure, transient fault handling, execution strategy, pool exhaustion fixes, timeouts, deadlock/serialization retry, or asks "how do I handle transient DB errors / make my DB calls resilient / tune my connection pool". Always use this skill for DB resiliency requests; it wires retry correctly including the transaction caveat.
category: Database
version: 1.0.0
---

# Database Resiliency Setup

Make data access survive transient database failures (timeouts, failovers, throttling, deadlocks, serialization conflicts) with bounded retry, sane timeouts, and a correctly sized pool - and do it correctly, including the transaction caveat that trips people up in every stack.

## Input - point it at your project
Works on a target: a **file**, **folder**, **project/repo**, or **GitHub URL**. Report what you found: driver/ORM, current pool settings, existing retry code, and every place that opens a manual transaction.

## Step 1 - detect the stack and data access

| Manifest | Look for | Retry building block | Pool lives in |
|---|---|---|---|
| `*.csproj` | `Microsoft.EntityFrameworkCore.*`, `Dapper`, `Npgsql`, `Microsoft.Data.SqlClient` | EF execution strategy; Polly / `Microsoft.Extensions.Resilience` for non-EF | connection string (`Maximum Pool Size`, `Timeout`) |
| `package.json` | `pg`, `@prisma/client`, `typeorm`, `drizzle-orm`, `mysql2`, `knex` | `cockatiel`, `p-retry` | `new Pool({...})`, Prisma URL params, TypeORM `extra` |
| `pyproject.toml`, `requirements*.txt` | `sqlalchemy`, `psycopg`, `asyncpg`, `django` | `tenacity` | `create_engine(...)`, `CONN_MAX_AGE`, psycopg pool |
| `pom.xml`, `build.gradle(.kts)` | `spring-boot-starter-data-jpa`, `HikariCP`, `jdbc` | Resilience4j `Retry`, Spring Retry `@Retryable` | `spring.datasource.hikari.*` |
| `go.mod` | `database/sql`, `pgx`, `gorm` | `cenkalti/backoff`, `avast/retry-go`, or a small loop | `db.SetMaxOpenConns(...)`, `pgxpool.Config` |

```
grep -rn "EnableRetryOnFailure\|BeginTransaction\|UseNpgsql\|UseSqlServer" --include=*.cs <target>
grep -rn "new Pool(\|connection_limit\|\$transaction\|retry" --include=*.ts --include=*.js <target> | grep -v node_modules
grep -rn "create_engine\|pool_size\|@retry\|tenacity\|transaction.atomic\|begin()" --include=*.py <target>
grep -rn "hikari\|@Retryable\|@Transactional\|Retry\b" <target>/src
grep -rn "SetMaxOpenConns\|pgxpool\|BeginTx\|backoff" --include=*.go <target>
```

## Universal checklist
1. **Retry only transient errors.** Connection drops, timeouts, failover, throttling, deadlock victim, serialization failure. Never retry constraint violations, syntax errors, or auth failures. Common codes: PostgreSQL SQLSTATE `40001`, `40P01`, class `08` (connection); SQL Server `1205` (deadlock) plus Azure SQL transient codes; MySQL `1213` (deadlock), `1205` (lock wait timeout).
2. **Bounded retries with exponential backoff and jitter.** 3-5 attempts, capped delay. Infinite or immediate retries turn a blip into an outage.
3. **Own the retry boundary.** Retry the whole unit of work (open transaction, do all statements, commit), never a single statement inside an open transaction - after a failure the transaction is gone.
4. **Idempotency.** A retry after a lost commit acknowledgement can double-apply. Use unique keys / idempotency keys / upserts / an outbox so re-running is safe.
5. **Timeouts at every layer.** Connect timeout, statement/command timeout, pool acquire timeout - and keep them shorter than the caller's request timeout.
6. **Pool sizing.** Total connections = instances x pool max; keep it under the server's `max_connections` minus headroom. Bigger is not faster - a small pool plus a queue usually beats a huge pool. Use PgBouncer/RDS Proxy when instance counts are high or serverless.
7. **Don't stack retries.** ORM retry + driver retry + app retry + HTTP client retry multiply. Pick one layer for DB calls.
8. **Observe it.** Log each retry with attempt number and error code; alert on pool wait time and exhaustion.

## C# / .NET
EF Core built-in execution strategy:
```csharp
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(conn, npg =>
    {
        npg.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
        npg.CommandTimeout(30);
    }));                     // SQL Server: o.UseSqlServer(conn, s => s.EnableRetryOnFailure());
```
**The transaction caveat:** with a retrying execution strategy you cannot wrap several `SaveChanges`/commands in a manual `BeginTransaction` directly - EF throws, because the strategy must own the retry boundary. Run the whole unit through the strategy:
```csharp
var strategy = db.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    await using var tx = await db.Database.BeginTransactionAsync(ct);
    // ... multiple operations ...
    await db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
});
```
For Dapper/ADO.NET, wrap the unit of work in a Polly (or `Microsoft.Extensions.Resilience`) retry pipeline that handles only transient `NpgsqlException.IsTransient` / `SqlException` numbers. Pool: `Maximum Pool Size`, `Timeout` (acquire), `Command Timeout` in the connection string. Don't add Polly around calls EF already guards.

## TypeScript / Node
- `pg`: `new Pool({ max, idleTimeoutMillis, connectionTimeoutMillis, statement_timeout })`. Always `release()` clients in `finally`.
- Prisma: `connection_limit` and `pool_timeout` as connection URL params; interactive `$transaction(async tx => ..., { timeout, maxWait })`. Retry the whole `$transaction` callback on `P2034` (write conflict/deadlock).
- Retry with `cockatiel` (`retry(handleWhen(isTransient), { maxAttempts, backoff: new ExponentialBackoff() })`, composable with circuit breaker and timeout) or `p-retry` (throw `AbortError` for non-retryable errors).

## Python
- SQLAlchemy: `create_engine(url, pool_size=..., max_overflow=..., pool_timeout=..., pool_recycle=..., pool_pre_ping=True)`; statement timeout via driver `connect_args` (e.g. Postgres `options="-c statement_timeout=30000"`).
- Retry with `tenacity`: `@retry(retry=retry_if_exception(is_transient), stop=stop_after_attempt(5), wait=wait_random_exponential(max=10))` on the function that opens the session/transaction and commits - not on a function called inside one.
- Django: `CONN_MAX_AGE`, `CONN_HEALTH_CHECKS = True` (4.1+); wrap `transaction.atomic()` blocks, not ORM calls inside them.

## Java / Kotlin (Spring)
- HikariCP: `maximum-pool-size`, `connection-timeout`, `max-lifetime` (below the DB/proxy idle cutoff), `leak-detection-threshold`.
- Retry with Resilience4j `Retry` (`retryExceptions` / `retryOnException` limited to transient types, exponential backoff) or Spring Retry `@Retryable`. **Caveat:** the retry advice must sit **outside** `@Transactional` so each attempt gets a fresh transaction - put `@Retryable` on a caller of the transactional method, or order the aspects so retry wraps the transaction.
- Spring's `TransientDataAccessException` / `CannotAcquireLockException` / `PessimisticLockingFailureException` are reasonable retry triggers.

## Go
- `database/sql`: `db.SetMaxOpenConns(n)`, `SetMaxIdleConns`, `SetConnMaxLifetime`, `SetConnMaxIdleTime`; pass a `context.WithTimeout` to every `QueryContext`/`ExecContext`/`BeginTx`. pgx: `pgxpool.Config` (`MaxConns`, `MaxConnLifetime`, `HealthCheckPeriod`).
- Retry the full `BeginTx ... Commit` function with `cenkalti/backoff` or `avast/retry-go`, classifying errors via `errors.As(err, &pgErr)` and the SQLSTATE code; respect `ctx.Done()` between attempts.

## Output
```
## Summary
<stack + driver/ORM + DB; current state of retry, timeouts, pool>

## Findings
🔴 [Retry inside transaction] <file:line> - <risk> -> <fix>
🟡 [Pool too large for 8 instances] ...

## Resiliency setup
<config + code in the project's idiom, ready to paste>

## Verify on your side
- <max_connections vs total pool, failover test, idempotency of retried writes>
```

## Principles
- **Retry transient faults, not logic errors.**
- **Own the retry boundary:** retry whole transactions, never statements inside one.
- **Cap retries and back off** - infinite retries turn a blip into an outage.
- **Idempotency first** - a safe retry is one that can run twice.
- One retry layer per dependency; app-level resiliency (Polly, cockatiel, tenacity, Resilience4j) is for what the DB driver/ORM doesn't already guard.

## How to use it & best prompts
"Add connection retry to my DbContext", "handle transient DB failures in my Node service", "my multi-step transaction breaks with retry enabled - fix it", "size the HikariCP pool for 6 pods", "make these SQLAlchemy writes resilient and idempotent". Pairs with `orm-config-auditor`.
