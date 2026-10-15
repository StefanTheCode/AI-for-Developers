---
name: structured-logging-setup
description: Set up structured (JSON) logging in any stack - enrichment/context, request logging, log levels, redaction, sinks, and log-trace correlation. Covers Serilog and Microsoft.Extensions.Logging (.NET), pino and winston (Node.js), structlog and stdlib logging (Python), SLF4J + Logback / Spring Boot structured logging (Java/Kotlin), slog, zap and zerolog (Go). Use this skill whenever the user wants structured logging, JSON logs, request/access logging, log enrichment, log context, log sinks or shipping (Seq, Elasticsearch, Loki, Datadog, CloudWatch), correlating logs with traces, or asks "how do I add logging / set up Serilog / pino / structlog / Logback / slog / get structured logs". Always use this skill for structured-logging requests; it wires production-shaped logging, not just print statements.
category: Observability
version: 1.0.0
---

# Structured Logging Setup

Wire up structured (JSON) logs with context enrichment, one tidy line per request, sensible levels, redaction, and a sink - correlated with your traces. The rules are the same in every language; only the library differs.

## Input - point it at your project
Works on a target: a **project**, a **folder**, or a **GitHub URL** (`git clone --depth 1 <url> /tmp/repo`). Inspect the current logging setup before generating anything. If OpenTelemetry is present, wire log/trace correlation.

## Step 1 - detect the stack and current logger

| Manifest | Stack | Look for | Recommended |
|---|---|---|---|
| `*.csproj` | .NET | `Serilog`, `UseSerilog`, `AddSerilog`, `ILogger<`, `AddJsonConsole` | Serilog (or built-in JSON console for minimal setups) |
| `package.json` | Node / TS | `pino`, `pino-http`, `winston`, `console.log` | pino (+ `pino-http`); keep winston if already used |
| `pyproject.toml`, `requirements*.txt` | Python | `structlog`, `logging.basicConfig`, `python-json-logger`, `loguru` | structlog (or stdlib + JSON formatter) |
| `pom.xml`, `build.gradle(.kts)` | Java / Kotlin | `logback`, `log4j2`, `logstash-logback-encoder`, `logging.structured` | SLF4J + Logback with JSON output |
| `go.mod` | Go | `log/slog`, `go.uber.org/zap`, `github.com/rs/zerolog` | `log/slog` (stdlib); keep zap/zerolog if present |

```
grep -rn "UseSerilog\|AddSerilog\|Log.Logger\|AddJsonConsole" --include=*.cs <target>
grep -rn "pino\|winston\|console\.log" --include=*.ts --include=*.js <target> | grep -v node_modules
grep -rn "structlog\|logging\.\(basicConfig\|getLogger\)\|print(" --include=*.py <target>
grep -rn "LoggerFactory\|@Slf4j\|logback\|System.out.println" <target>/src <target>/src/main/resources 2>/dev/null
grep -rn "slog\.\|zap\.\|zerolog\|log\.Printf" --include=*.go <target>
```

## Universal rules (apply in every stack)

1. **Structured fields, not interpolated strings.** `"Order {OrderId} placed", id` or `log.info("order placed", order_id=id)` - never `f"Order {id} placed"`. Structure is the whole point: fields are queryable, interpolated text is not.
2. **JSON to stdout** in containers; let the platform (Docker, k8s, a collector, Fluent Bit, Vector) ship it. Human-readable console only in local dev.
3. **Context enrichment once, not per call.** Service name, environment, version, and a correlation/trace id attached via log context (LogContext / MDC / contextvars / child logger / `context.Context`), so every line carries them.
4. **One line per request** with method, route template, status, duration - replace the framework's noisy default request logs.
5. **Levels:** `Info` for business events, `Warn` for recoverable oddities, `Error` with the **exception object** (stack trace), `Debug` only in dev. Turn noisy framework namespaces down to `Warn`. Configure via config/env, not code.
6. **Trace correlation:** every line includes `trace_id`/`span_id` when OTel is active (see `opentelemetry-setup`).
7. **Redact secrets and PII** at the logger (Authorization headers, tokens, passwords, emails as policy dictates). Never log full request bodies by default.
8. **Consistent field names** across services (`trace_id`, `service`, `env`, `duration_ms`) so queries work everywhere.

## C# / .NET - Serilog

```
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Enrichers.Environment   # WithMachineName
dotnet add package Serilog.Sinks.Seq               # or Elasticsearch / Grafana Loki / OpenTelemetry sink
```
```csharp
builder.Host.UseSerilog((ctx, services, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("service", "orders")
    .WriteTo.Console(new RenderedCompactJsonFormatter())   // structured JSON
    .WriteTo.Seq(ctx.Configuration["Seq:Url"]!));

app.UseSerilogRequestLogging();   // one tidy log line per request instead of the default noise
```
```csharp
// GOOD: message template + properties (queryable)
logger.LogInformation("Order {OrderId} placed by {CustomerId} for {Total}", id, customerId, total);
// BAD: interpolated string (loses structure)
logger.LogInformation($"Order {id} placed");
// Errors: pass the exception, not ex.Message
logger.LogError(ex, "Payment failed for {OrderId}", id);
```
- Levels via `appsettings.json` `Serilog:MinimumLevel` with overrides for `Microsoft` / `Microsoft.AspNetCore` set to `Warning`.
- Trace correlation: with OTel active, `Activity.Current` carries `TraceId`/`SpanId`; recent Serilog versions capture them on events - verify they appear in the JSON output, and enrich explicitly if not.
- Hot paths: `LoggerMessage` source generators (`[LoggerMessage]`) avoid boxing and template parsing.
- No Serilog wanted? `builder.Logging.AddJsonConsole()` gives structured JSON with built-in logging and `BeginScope` for context.

## TypeScript / Node.js - pino

```ts
import pino from 'pino';
import pinoHttp from 'pino-http';

export const logger = pino({
  level: process.env.LOG_LEVEL ?? 'info',
  base: { service: 'orders', env: process.env.NODE_ENV },
  redact: ['req.headers.authorization', 'req.headers.cookie', '*.password'],
});
app.use(pinoHttp({ logger }));          // one line per request, req.log child logger

logger.info({ orderId, customerId, total }, 'order placed');   // fields first, message second
logger.error({ err, orderId }, 'payment failed');              // pino serializes `err` with stack
```
- Context per request: use `req.log` (child logger) or `AsyncLocalStorage` for code outside the handler.
- Pretty output only in dev (`pino-pretty` as a dev transport); JSON in prod.
- winston: `format.combine(format.timestamp(), format.errors({ stack: true }), format.json())`.
- Trace correlation: `@opentelemetry/instrumentation-pino` / `-winston` inject `trace_id`/`span_id`.

## Python - structlog

```python
import logging, structlog

structlog.configure(
    processors=[
        structlog.contextvars.merge_contextvars,
        structlog.processors.add_log_level,
        structlog.processors.TimeStamper(fmt="iso"),
        structlog.processors.dict_tracebacks,
        structlog.processors.JSONRenderer(),
    ],
    wrapper_class=structlog.make_filtering_bound_logger(logging.INFO),
)
log = structlog.get_logger(service="orders")

log.info("order placed", order_id=order_id, customer_id=customer_id, total=total)
log.exception("payment failed", order_id=order_id)    # inside except: includes the traceback
```
- Request context: middleware calls `structlog.contextvars.clear_contextvars()` then `bind_contextvars(request_id=..., route=...)`.
- Stdlib-only: `logging` + `python-json-logger`'s JSON formatter; use `extra={...}` for fields, `logger.exception(...)` for errors.
- Access logs: disable uvicorn/gunicorn's default access log if your middleware emits one line per request.
- Trace correlation: OTel's logging instrumentation adds `otelTraceID`/`otelSpanID` to stdlib records; for structlog add a processor that reads the current span context.

## Java / Kotlin - SLF4J + Logback

- **Spring Boot 3.4+:** built-in structured logging - `logging.structured.format.console=ecs` (or `logstash`, `gelf`).
- **Otherwise:** `net.logstash.logback:logstash-logback-encoder` with `<encoder class="net.logstash.logback.encoder.LogstashEncoder"/>` in `logback-spring.xml`.
```java
log.info("Order placed {}", kv("orderId", orderId));                       // logstash StructuredArguments
log.atInfo().addKeyValue("orderId", orderId).log("Order placed");           // SLF4J 2 fluent API
log.error("Payment failed for order {}", orderId, ex);                      // exception last = stack trace
```
- Context: `MDC.put("correlationId", id)` in a servlet filter / WebFlux filter, cleared in `finally`. MDC is thread-bound - propagate it explicitly across executors and reactive chains.
- Request logging: a filter that logs once per request, or the access log of the server; turn `org.springframework` and `org.hibernate` down to `WARN`.
- Trace correlation: the OTel Java agent / Micrometer Tracing put `trace_id`/`span_id` (or `traceId`/`spanId`) into the MDC.

## Go - log/slog

```go
logger := slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{Level: slog.LevelInfo}))
logger = logger.With("service", "orders", "env", os.Getenv("ENV"))
slog.SetDefault(logger)

slog.InfoContext(ctx, "order placed", "order_id", id, "total", total)
slog.ErrorContext(ctx, "payment failed", "order_id", id, "err", err)
```
- Request logging: middleware that wraps `http.ResponseWriter` to capture status and logs once with duration.
- Context: pass `ctx` and use the `*Context` methods; a custom handler (or the `otelslog` bridge) pulls `trace_id` from the span context.
- zap (`zap.NewProduction()`, `zap.String(...)` fields) and zerolog (`log.Info().Str("order_id", id).Msg("order placed")`) are fine if already in use - don't migrate for its own sake.

## Output format

```
## Current state
<stack, logger found, what's missing>

## Packages
<install commands>

## Wiring
<logger config + request logging + context enrichment + redaction>

## Usage examples
<good vs bad log calls in their code style>

## Config
<levels per environment, sink settings>
```

## How to use it & best prompts
"Set up Serilog with JSON logs and Seq", "add pino with request logging to my Express app", "configure structlog for FastAPI", "make Logback emit JSON in Spring Boot", "switch my Go service to slog with trace ids", "correlate my logs with traces". Pairs with `opentelemetry-setup` and `correlation-id-middleware`.
