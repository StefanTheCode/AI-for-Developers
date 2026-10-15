---
name: healthchecks-setup
description: Add health checks with a correct liveness/readiness (and startup) split plus dependency checks (database, cache, message broker, downstream APIs) for Kubernetes, ECS, and load balancers - in any stack. Covers ASP.NET Core health checks, Spring Boot Actuator probes, NestJS Terminus / Express, FastAPI / Django, and Go net/http or gRPC health. Use this skill whenever the user wants health checks, liveness/readiness/startup probes, /health or /healthz endpoints, actuator health, dependency health, Kubernetes probes, graceful shutdown with readiness, or asks "how do I add health checks / readiness vs liveness / why do my pods restart when the DB blips". Always use this skill for health-check requests; it wires the live/ready split and dependency checks correctly.
category: Observability
version: 1.0.0
---

# Health Checks Setup

Add health checks that orchestrators can actually use: a **liveness** probe (is the process alive?) separate from a **readiness** probe (can it serve traffic right now?), optionally a **startup** probe, with dependency checks attached to the right one.

## Input - point it at your project
Works on a target: a **project** or **GitHub URL**. Inspect existing health endpoints and the dependencies in the manifest (DB driver, cache client, broker client, outbound APIs) to know what to check.

## Step 1 - detect the stack

| Manifest | Stack | Look for | Use |
|---|---|---|---|
| `*.csproj` | ASP.NET Core | `AddHealthChecks`, `MapHealthChecks`, `AspNetCore.HealthChecks.*` | built-in health checks + `AspNetCore.HealthChecks.*` packages |
| `pom.xml`, `build.gradle(.kts)` | Spring Boot | `spring-boot-starter-actuator`, `management.endpoint.health` | Actuator health groups + probes |
| `package.json` | NestJS / Express / Fastify | `@nestjs/terminus`, `@godaddy/terminus`, `/health` routes | `@nestjs/terminus` (Nest) or plain routes / `@godaddy/terminus` |
| `pyproject.toml`, `requirements*.txt` | FastAPI / Django / Flask | `/health`, `django-health-check` | plain routes (FastAPI/Flask), `django-health-check` (Django) |
| `go.mod` | Go | `/healthz`, `grpc/health` | plain handlers; `google.golang.org/grpc/health` for gRPC services |

```
grep -rn "AddHealthChecks\|MapHealthChecks" --include=*.cs <target>
grep -rn "actuator\|management.endpoint" <target>/pom.xml <target>/build.gradle* <target>/src/main/resources 2>/dev/null
grep -rn "terminus\|/health" --include=*.ts --include=*.js <target> | grep -v node_modules
grep -rn "health" --include=*.py <target>
grep -rn "healthz\|grpc/health" --include=*.go <target>
```

## Universal rules (the part that matters)

| Probe | Answers | Includes dependencies? | On failure the orchestrator... |
|---|---|---|---|
| **Liveness** | Is the process stuck/dead? | **No** - process-local only | restarts the container |
| **Readiness** | Can I serve traffic now? | **Yes** - DB, cache, broker, critical downstreams | stops routing traffic, no restart |
| **Startup** | Have I finished booting (migrations, warm-up)? | optional | holds off liveness until it passes |

- **Liveness must NOT depend on the DB** (or any network dependency). A transient DB outage would restart every pod at once and turn a blip into an outage.
- **Readiness includes critical dependencies only.** A non-critical dependency (e.g. a recommendations API with a fallback) should report degraded, not fail readiness.
- **Fast and cheap:** short per-check timeouts (well under the probe timeout), no heavy queries (`SELECT 1`, `PING`), cache results briefly if checks are expensive.
- **Graceful shutdown:** on SIGTERM, fail readiness first, drain in-flight requests, then exit.
- **Don't expose detailed health publicly** without auth - it leaks topology and versions. Probes get status codes; details go to an internal/authenticated endpoint.
- HTTP 200 = healthy, 503 = unhealthy. Keep probe paths stable.

## C# / .NET - ASP.NET Core

```
dotnet add package AspNetCore.HealthChecks.NpgSql
dotnet add package AspNetCore.HealthChecks.Redis
dotnet add package AspNetCore.HealthChecks.UI.Client   # JSON response writer
```
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(conn, name: "db", tags: new[] { "ready" })
    .AddRedis(redisConn, name: "redis", tags: new[] { "ready" })
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

// Liveness: is the process up? (no dependencies - don't restart on a DB blip)
app.MapHealthChecks("/health/live", new() { Predicate = c => c.Tags.Contains("live") });

// Readiness: can it serve? (include dependencies - pull from LB while a dep is down)
app.MapHealthChecks("/health/ready", new()
{
    Predicate = c => c.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse   // JSON detail
});
```
- EF Core: `.AddDbContextCheck<AppDbContext>(tags: new[] { "ready" })` (`Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`).
- Restrict the detailed endpoint with `.RequireAuthorization()` or `.RequireHost(...)`.

## Java / Kotlin - Spring Boot Actuator

```properties
management.endpoint.health.probes.enabled=true          # /actuator/health/liveness and /readiness
management.endpoint.health.group.readiness.include=readinessState,db,redis
management.endpoint.health.group.liveness.include=livenessState
management.endpoint.health.show-details=when-authorized
management.endpoints.web.exposure.include=health
```
- Actuator auto-detects `DataSource`, Redis, Rabbit, Kafka, etc. Make sure they land in the **readiness** group, never liveness.
- Custom check: a `@Component` implementing `HealthIndicator`; add its name to the readiness group.
- Probes are enabled automatically on Kubernetes; set them explicitly so local and k8s behave the same. Consider a separate `management.server.port` to keep actuator off the public port.

## TypeScript / Node.js

NestJS - `@nestjs/terminus`:
```ts
@Controller('health')
export class HealthController {
  constructor(private health: HealthCheckService, private db: TypeOrmHealthIndicator) {}
  @Get('live') @HealthCheck() live() { return this.health.check([]); }
  @Get('ready') @HealthCheck() ready() { return this.health.check([() => this.db.pingCheck('db', { timeout: 1000 })]); }
}
```
Express / Fastify - plain routes:
```ts
let shuttingDown = false;
app.get('/health/live', (_req, res) => res.sendStatus(200));
app.get('/health/ready', async (_req, res) => {
  if (shuttingDown) return res.sendStatus(503);
  try {
    await Promise.all([withTimeout(pool.query('SELECT 1'), 1000), withTimeout(redis.ping(), 1000)]);
    res.sendStatus(200);
  } catch { res.sendStatus(503); }
});
process.on('SIGTERM', () => { shuttingDown = true; server.close(); });
```
`@godaddy/terminus` packages health checks plus graceful shutdown for plain Node HTTP servers.

## Python - FastAPI / Django

```python
@app.get("/health/live")
async def live():
    return {"status": "ok"}

@app.get("/health/ready")
async def ready(response: Response):
    try:
        async with asyncio.timeout(1):            # Python 3.11+; use asyncio.wait_for on older versions
            await db.execute(text("SELECT 1"))
            await redis.ping()
        return {"status": "ok"}
    except Exception:
        response.status_code = 503
        return {"status": "unavailable"}
```
Django: `django-health-check` provides DB/cache/storage checks; expose a dependency-free view for liveness and the package's view for readiness.

## Go - net/http (and gRPC)

```go
var ready atomic.Bool // set true after startup, false on SIGTERM

http.HandleFunc("/healthz", func(w http.ResponseWriter, _ *http.Request) { w.WriteHeader(http.StatusOK) })
http.HandleFunc("/readyz", func(w http.ResponseWriter, r *http.Request) {
	ctx, cancel := context.WithTimeout(r.Context(), time.Second)
	defer cancel()
	if !ready.Load() || db.PingContext(ctx) != nil || rdb.Ping(ctx).Err() != nil {
		w.WriteHeader(http.StatusServiceUnavailable)
		return
	}
	w.WriteHeader(http.StatusOK)
})
```
gRPC services: register `google.golang.org/grpc/health` and use Kubernetes' native `grpc` probe type.

## Kubernetes probes (all stacks)

```yaml
startupProbe:   { httpGet: { path: /health/live,  port: 8080 }, periodSeconds: 5, failureThreshold: 30 }
livenessProbe:  { httpGet: { path: /health/live,  port: 8080 }, periodSeconds: 10 }
readinessProbe: { httpGet: { path: /health/ready, port: 8080 }, periodSeconds: 5 }
```
Adjust paths per stack (Spring: `/actuator/health/liveness` and `/actuator/health/readiness`; Go convention: `/healthz`, `/readyz`). Docker Compose / ECS: point the container `healthcheck` at the readiness endpoint.

## Output format

```
## Current state
<stack, existing endpoints, dependencies found>

## Wiring
<liveness, readiness (+startup) endpoints and dependency checks>

## Orchestrator config
<k8s probes / compose healthcheck / LB target group path>

## Notes
<what was deliberately kept out of liveness, auth on detailed output>
```

## How to use it & best prompts
"Add health checks with liveness and readiness", "wire DB and Redis into readiness", "Kubernetes probes for my Spring Boot / Node / Go service", "why does a DB blip restart my pods" (answer: liveness depends on the DB - fix the split). Pairs with `microservice-template-generator`.
