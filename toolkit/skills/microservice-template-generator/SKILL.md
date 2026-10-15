---
name: microservice-template-generator
description: Generate a production-shaped microservice template in any stack - health checks (liveness/readiness), OpenTelemetry tracing and metrics, structured JSON logging, validated typed config, graceful shutdown, Docker, and a messaging consumer/producer stub - for ASP.NET Core, Node (Fastify/Express/NestJS), Python (FastAPI), Java/Kotlin (Spring Boot + Actuator), or Go (net/http/chi + slog). Use this skill whenever the user wants a new microservice, a service template/scaffold/starter/skeleton, a service with observability and health checks, or asks "scaffold a microservice / give me a service template / new service matching our others". Always use this skill for microservice-template requests; it wires the production essentials, not just a Hello World.
category: Architecture
version: 1.0.0
---

# Microservice Template Generator

Scaffold a new service with the production essentials wired from day one - health checks, telemetry, structured logs, validated config, graceful shutdown, Docker, and a messaging stub - so a new service starts at "production-ready", not "Hello World".

## Input - point it at your workspace
For a new service, take the name, responsibility, and stack. For an existing platform, **match the house template**: point at the repo (path or **GitHub URL**) and read an existing service first (entry point, shared libraries/packages, Dockerfile, CI, deployment manifests) so the new one is consistent.

## Step 1 - detect (or choose) the stack

| Manifest | Stack | Default building blocks |
|---|---|---|
| `*.csproj` | ASP.NET Core | Health checks, OpenTelemetry .NET, Serilog, Options + `ValidateOnStart`, MassTransit or a broker client |
| `package.json` | Node / TypeScript | Fastify (or the project's Express/NestJS), pino, `@opentelemetry/sdk-node` + auto-instrumentations, zod-validated env, kafkajs / amqplib / nats |
| `pyproject.toml` / `requirements*.txt` | Python | FastAPI + uvicorn, structlog, OpenTelemetry FastAPI instrumentation, pydantic-settings, aiokafka / aio-pika / FastStream |
| `pom.xml` / `build.gradle(.kts)` | Java / Kotlin | Spring Boot Actuator probes, Micrometer + OTLP (or the OTel Java agent), structured JSON logging (Logback), `@ConfigurationProperties` + `@Validated`, Spring Kafka / Spring AMQP |
| `go.mod` | Go | net/http or chi, `log/slog` JSON handler, OpenTelemetry Go + `otelhttp`, env config (caarlos0/env or envconfig), nats.go / franz-go / Watermill |

In a greenfield monorepo, ask which stack if it's not obvious; don't pick one silently.

## Universal checklist (every service gets all of these)
1. **Health endpoints, split:** liveness (process is up, no dependency checks) and readiness (can serve traffic: DB, broker reachable). Kubernetes probes point at them.
2. **Telemetry:** OpenTelemetry traces + metrics exported via OTLP, `service.name` / version / environment as resource attributes, inbound HTTP and outbound clients instrumented.
3. **Structured JSON logs** to stdout, with trace/span IDs correlated and no secrets.
4. **Typed, validated config** from environment / secret store; fail fast at startup on missing or invalid values (12-factor; no secrets committed).
5. **Graceful shutdown:** on SIGTERM stop accepting traffic, drain in-flight requests and consumers, close connections, then exit within the termination grace period.
6. **Messaging stub:** one consumer and one producer, idempotent consumer handling (dedupe on message ID), retries with a dead-letter path, outbox note for producers that also write to a DB.
7. **Docker:** multi-stage build, small runtime image, non-root user, `.dockerignore`.
8. **Resilience defaults:** timeouts on every outbound call; retries only where idempotent.
9. **README:** what the service owns, how to run locally, config table, endpoints.

## C# / .NET
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<ServiceOptions>().Bind(builder.Configuration.GetSection("Service")).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddHealthChecks().AddNpgSql(conn, tags: ["ready"]);   // AspNetCore.HealthChecks.NpgSql
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console(new RenderedCompactJsonFormatter()));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("orders-service"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddOtlpExporter())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation().AddOtlpExporter());

var app = builder.Build();
app.MapHealthChecks("/health/live",  new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
app.Run();
```
Plus: a multi-stage `Dockerfile`, `appsettings` template, a messaging consumer/producer stub (MassTransit, NATS, or the raw broker client), and a `README`. ASP.NET Core handles SIGTERM via the host; set `HostOptions.ShutdownTimeout` to fit the grace period. In an Aspire-based platform, reuse the shared ServiceDefaults project instead of repeating this wiring.
```
OrdersService/
├── Program.cs
├── Endpoints/        ├── Domain/        ├── Infrastructure/
├── Dockerfile        ├── appsettings.json
└── OrdersService.csproj
```

## TypeScript / Node
- `src/instrumentation.ts` starts `NodeSDK` from `@opentelemetry/sdk-node` with `getNodeAutoInstrumentations()` and an OTLP exporter; load it before the app (`node --import ./dist/instrumentation.js dist/main.js`, or `--require` for CommonJS).
- `src/config.ts` parses `process.env` with a zod schema and exits on failure.
- Fastify with its built-in pino logger (`logger: true` / pino options), `GET /health/live` and `GET /health/ready` routes; readiness pings DB and broker.
- Graceful shutdown: on `SIGTERM`/`SIGINT` call `await app.close()`, stop consumers, close pools, then exit.
- Dockerfile: `node:<lts>-slim` build stage, `npm ci --omit=dev` runtime stage, `USER node`.

## Python
- FastAPI app with `/health/live` and `/health/ready`; settings via a `pydantic-settings` `BaseSettings` class (fails on startup when invalid).
- structlog configured for JSON output with a processor that adds trace/span IDs.
- OpenTelemetry: `opentelemetry-instrumentation-fastapi` (+ httpx/SQLAlchemy instrumentations) with OTLP exporter, or run under `opentelemetry-instrument`.
- Lifespan handler (`@asynccontextmanager` passed as `lifespan=`) opens and closes DB pools and starts/stops consumers; uvicorn handles SIGTERM.
- Dockerfile: slim Python base, dependencies installed in a build stage (or `uv`), non-root user, `uvicorn app.main:app --host 0.0.0.0`.

## Java / Kotlin - Spring Boot
```yaml
# application.yml
management:
  endpoint.health.probes.enabled: true          # /actuator/health/liveness and /readiness
  endpoints.web.exposure.include: health,info,prometheus
  tracing.sampling.probability: 1.0
  otlp.tracing.endpoint: ${OTLP_TRACES_ENDPOINT}
server.shutdown: graceful
spring.lifecycle.timeout-per-shutdown-phase: 20s
```
- Config: `@ConfigurationProperties(prefix = "orders")` record + `@Validated` with Bean Validation.
- Tracing via Micrometer Tracing with the OTel bridge and OTLP exporter (or attach the OpenTelemetry Java agent if that's the platform standard).
- JSON logs: Spring Boot's built-in structured logging (`logging.structured.format.console`) on recent versions, otherwise logstash-logback-encoder.
- Messaging: Spring Kafka `@KafkaListener` / Spring AMQP `@RabbitListener` stub with a dead-letter setup.
- Dockerfile: multi-stage with a JRE base, or Buildpacks via `bootBuildImage` if the platform uses them.

## Go
```go
func main() {
    ctx, stop := signal.NotifyContext(context.Background(), syscall.SIGINT, syscall.SIGTERM)
    defer stop()

    cfg := mustLoadConfig()                                          // env -> struct, validated
    logger := slog.New(slog.NewJSONHandler(os.Stdout, nil))
    shutdownOTel := mustInitOTel(ctx, "orders-service")              // OTLP trace + metric providers
    defer shutdownOTel(context.Background())

    mux := http.NewServeMux()
    mux.HandleFunc("GET /health/live", func(w http.ResponseWriter, _ *http.Request) { w.WriteHeader(http.StatusOK) })
    mux.HandleFunc("GET /health/ready", readiness(db, broker))
    srv := &http.Server{Addr: cfg.Addr, Handler: otelhttp.NewHandler(mux, "http"), ReadHeaderTimeout: 5 * time.Second}

    go func() {
        if err := srv.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
            logger.Error("server failed", "err", err); stop()
        }
    }()
    <-ctx.Done()
    shutdownCtx, cancel := context.WithTimeout(context.Background(), 20*time.Second)
    defer cancel()
    _ = srv.Shutdown(shutdownCtx)                                     // drain in-flight requests
}
```
Dockerfile: `golang` build stage with `CGO_ENABLED=0`, distroless or `scratch` (+ CA certs) runtime, non-root user.

## Output format
```
## Detected
<stack, house template matched (or chosen defaults), broker>

## Files
<tree + full content: entry point, config, health, telemetry, logging, messaging stub, Dockerfile, .dockerignore, README>

## Config table
| Variable | Required | Default | Purpose |

## Run locally
<commands, incl. docker build/run and where telemetry goes>
```

## Principles
- **Production essentials are not optional:** health (live/ready split), telemetry, structured logs, validated config, graceful shutdown - wired before the first endpoint.
- **Match the platform.** A new service should look like the others; read one first and reuse shared libraries.
- **12-factor config** - environment/secret store, not committed config files with secrets.
- Don't gold-plate: include what every service needs, leave domain-specific bits to the feature skills.

## How to use it & best prompts
"Scaffold an orders microservice with NATS messaging", "give me a service template matching our existing services", "new .NET service with health checks, OTel, Serilog, Docker", "Fastify service template with pino and OpenTelemetry", "FastAPI service skeleton with structlog and Kafka consumer", "Spring Boot service with Actuator probes and OTLP", "Go service with slog, graceful shutdown, and NATS". Pair with `dockerfile-generator`, `healthchecks-setup`, `structured-logging-setup`, `opentelemetry-setup`, and `typed-config-generator` for deeper config.
