# How to use - Database Resiliency Setup

**What it is.** Adds database resiliency to any stack - bounded retry with backoff for transient faults, timeouts at every layer, sane connection pool sizing, and the correct pattern for retrying transactions (the caveat that breaks naive retry setups everywhere). It detects your stack first: EF Core execution strategy / Polly (.NET), cockatiel or p-retry with pg/Prisma pools (Node), tenacity with SQLAlchemy/Django (Python), Resilience4j or Spring Retry with HikariCP (Java/Kotlin), and database/sql or pgxpool with a backoff library (Go).

**When to reach for it.** Transient DB errors (timeouts, failovers, throttling, deadlocks), pool exhaustion, or after enabling retry and finding your transactions break.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Add connection retry to my DbContext."
- "My multi-step transaction breaks with retry enabled - fix it."
- "Retry Prisma transactions on write conflicts and tune the pool."
- "Make these SQLAlchemy writes resilient and idempotent."
- "Size HikariCP for 6 pods against a Postgres with max_connections=200."
- "Add retry with backoff around my Go `BeginTx` blocks."

**What you get.** Findings ranked by severity, paste-ready config and code in your project's idiom, and a "verify on your side" list (total connections vs server limit, failover test, idempotency of retried writes).

**Get the best out of it.** Let it find every manual transaction - those must be retried as a whole unit. Tell it how many instances/pods run and your DB's connection limit. Ask about idempotency if a unit of work might run twice. Pairs with `orm-config-auditor`.

**Won't do.** It won't retry logic errors (constraint violations, bad SQL) - only known transient faults - and it won't stack a second retry layer on calls your ORM already guards.
