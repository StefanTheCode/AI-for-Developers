---
name: opentelemetry-setup
description: Add or fix OpenTelemetry observability (traces, metrics, logs) in a service written in any stack - .NET/ASP.NET Core, Node.js/TypeScript, Python (FastAPI, Django, Flask), Java/Kotlin (Spring Boot, Java agent), Go. Use this skill whenever the user wants to instrument a service, add distributed tracing, export to Jaeger/Tempo/Prometheus/an OTLP collector/Azure Monitor/a vendor, create custom spans or metrics, set up correlation across services, or asks "how do I add observability/telemetry/tracing to my app", "set up OTel", "NodeSDK", "opentelemetry-instrument", "javaagent", "otelhttp", "AddOpenTelemetry". Always use this skill for OpenTelemetry requests; it generates wired-up, production-shaped configuration rather than a snippet.
category: Observability
---

# OpenTelemetry Setup

Wire up OpenTelemetry in a service - traces, metrics, and logs - exported to the backend the user wants, with sensible production defaults. The concepts (resource, providers, instrumentation, exporters, propagation, sampling) are identical across SDKs; only the wiring differs.

## Input - point it at your project

Don't ask the user to paste startup code - point this at the project and inspect what's already there:

- **A project or repo root** - find the entry point and any existing OTel wiring.
- **A folder or single file** - if they point directly at the startup file.
- **A GitHub URL** - clone first: `git clone --depth 1 <url> /tmp/repo && cd /tmp/repo`

If OTel is already partly set up (e.g. metrics but no tracing), **extend it** rather than starting over, and call out the specific gap.

## Step 1 - detect the stack

| Manifest | Stack | Look for existing wiring | Default approach |
|---|---|---|---|
| `*.csproj` | .NET / ASP.NET Core | `AddOpenTelemetry`, `WithTracing`, `AddOtlpExporter`, `OpenTelemetry.*` packages | SDK in `Program.cs` (below) |
| `package.json` | Node.js / TypeScript | `@opentelemetry/sdk-node`, `instrumentation.ts`, `--require`/`--import` flags | `NodeSDK` + auto-instrumentations |
| `pyproject.toml`, `requirements*.txt` | Python | `opentelemetry-distro`, `opentelemetry-instrument`, `TracerProvider` | zero-code `opentelemetry-instrument` |
| `pom.xml`, `build.gradle(.kts)` | Java / Kotlin | `opentelemetry-javaagent`, `opentelemetry-spring-boot-starter`, `micrometer-tracing` | Java agent (`-javaagent`) |
| `go.mod` | Go | `go.opentelemetry.io/otel`, `otelhttp`, `otelgrpc` | manual SDK + contrib instrumentation |

```
grep -rn "AddOpenTelemetry\|WithTracing\|AddOtlpExporter" --include=*.cs <target> | grep -v "/obj/\|/bin/"
grep -rn "@opentelemetry/" <target>/package.json <target>/src 2>/dev/null
grep -rni "opentelemetry" <target>/pyproject.toml <target>/requirements*.txt 2>/dev/null
grep -rn "opentelemetry\|micrometer-tracing" <target>/pom.xml <target>/build.gradle* 2>/dev/null
grep -rn "go.opentelemetry.io" <target>/go.mod
```

Then establish (ask only what you can't infer):
1. **App type** - HTTP API, worker/consumer, CLI/batch job, or library (libraries use only the API, never configure the SDK).
2. **Backend** - OTLP collector (vendor-neutral, default), Jaeger, Prometheus + Grafana/Tempo, Azure Monitor, a vendor (Datadog, Honeycomb, New Relic, Grafana Cloud all accept OTLP), or "console for now".
3. **Runtime version** - defaults assume current LTS (.NET 8/9, Node 20+, Python 3.10+, Java 17+, a supported Go release).

## Workflow

1. Confirm app type, backend, and signals (traces / metrics / logs - default all three).
2. Generate the wiring + dependencies for the detected stack.
3. Set **resource attributes** (`service.name`, `service.version`, `deployment.environment`) - non-negotiable.
4. Add instrumentation for the inbound framework, outbound HTTP/gRPC, DB driver/ORM, and runtime.
5. Show one custom span and one custom metric around the user's real domain operation.
6. Give the export config for the chosen backend, plus the collector/compose snippet if relevant.

## Universal config (env vars, same in every SDK)

```
OTEL_SERVICE_NAME=orders-api
OTEL_RESOURCE_ATTRIBUTES=service.version=1.4.2,deployment.environment=production
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317   # gRPC; http/protobuf uses :4318
OTEL_EXPORTER_OTLP_PROTOCOL=grpc                     # or http/protobuf
OTEL_TRACES_SAMPLER=parentbased_traceidratio
OTEL_TRACES_SAMPLER_ARG=0.1
```
Prefer env vars over hardcoded endpoints - same build runs against local Jaeger and production. Defaults differ per SDK (some default to http/protobuf on 4318), so set protocol and port explicitly.

## C# / .NET (ASP.NET Core, worker services)

```
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Instrumentation.Runtime
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```
Add as needed: `OpenTelemetry.Instrumentation.EntityFrameworkCore`, `OpenTelemetry.Exporter.Prometheus.AspNetCore`, `Azure.Monitor.OpenTelemetry.AspNetCore`.

```csharp
var builder = WebApplication.CreateBuilder(args);
const string serviceName = "my-service";
const string serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName, serviceVersion: serviceVersion)
        .AddAttributes(new[] {
            new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(o => o.RecordException = true)
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = true)
        .AddSource(serviceName) // your custom ActivitySource
        .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(1.0))) // tune ratio in prod
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(serviceName) // your custom Meter
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;
    logging.AddOtlpExporter();
});
```

Custom span and metric:
```csharp
private static readonly ActivitySource Activity = new("my-service");

public async Task PlaceOrderAsync(Order order, CancellationToken ct)
{
    using var span = Activity.StartActivity("PlaceOrder");
    span?.SetTag("order.id", order.Id);
    try { await _repo.SaveAsync(order, ct); }
    catch (Exception ex) { span?.SetStatus(ActivityStatusCode.Error, ex.Message); throw; }
}

public class OrderMetrics(IMeterFactory meterFactory)
{
    private readonly Counter<long> _placed =
        meterFactory.Create("my-service").CreateCounter<long>("orders.placed", unit: "{orders}");
    public void OrderPlaced(string region) => _placed.Add(1, new KeyValuePair<string, object?>("region", region));
}
```

.NET backend variants:
- **Prometheus:** add `OpenTelemetry.Exporter.Prometheus.AspNetCore`, use `.AddPrometheusExporter()` for metrics, and `app.MapPrometheusScrapingEndpoint();`.
- **Azure Monitor:** `builder.Services.AddOpenTelemetry().UseAzureMonitor();` (replaces exporters; set `APPLICATIONINSIGHTS_CONNECTION_STRING`).
- **.NET Aspire:** the ServiceDefaults project already wires this - extend it instead of duplicating.
- Gotcha: a custom `ActivitySource`/`Meter` is invisible until registered with `AddSource`/`AddMeter`.

## Other stacks (summary - full code in `references/otel-per-stack.md`)

| Stack | Setup | Instrumentation | Custom span | Custom metric |
|---|---|---|---|---|
| Node / TS | `@opentelemetry/sdk-node` `NodeSDK` in `instrumentation.ts`, loaded before the app (`node --import`/`--require`) | `@opentelemetry/auto-instrumentations-node` (http, express, fastify, pg, mysql, redis, ...) | `trace.getTracer('svc').startActiveSpan(...)` | `metrics.getMeter('svc').createCounter(...)` |
| Python | `pip install opentelemetry-distro opentelemetry-exporter-otlp`, `opentelemetry-bootstrap -a install`, run via `opentelemetry-instrument <cmd>` | auto (FastAPI, Django, Flask, requests, httpx, SQLAlchemy, psycopg, ...) | `with tracer.start_as_current_span(...)` | `meter.create_counter(...)` |
| Java / Kotlin | `java -javaagent:opentelemetry-javaagent.jar -jar app.jar`; or the OpenTelemetry Spring Boot starter (native image, no agent) | agent covers Spring MVC/WebFlux, JDBC, Hibernate, Kafka, gRPC, HTTP clients | `@WithSpan` or `tracer.spanBuilder(...)` | `meter.counterBuilder(...)` |
| Go | manual: `go.opentelemetry.io/otel` + SDK + OTLP exporter; set provider and propagator in `main` | contrib: `otelhttp`, `otelgrpc`, `otelsql`/driver wrappers | `otel.Tracer("svc").Start(ctx, ...)` | `otel.Meter("svc").Int64Counter(...)` |

Stack gotchas:
- **Node:** the SDK must load before `express`/`http` are imported, or auto-instrumentation silently misses them. ESM apps need the `--import` hook path.
- **Python:** `opentelemetry-instrument` doesn't work with reload mode; for pre-fork servers (gunicorn, uWSGI) initialize the SDK per worker (post-fork hook), not in the master.
- **Java:** the agent instruments a lot by default - disable noisy instrumentations rather than raising sampling. Spring Boot 3 apps already on Micrometer can bridge via Micrometer Tracing with the OTel bridge instead.
- **Go:** no auto-magic. Every hop needs `ctx` passed through and wrapped handlers/transports; a missing `ctx` is a broken trace. Call `tp.Shutdown(ctx)` on exit or buffered spans are lost.

## Backends

- **OTLP collector** (default) - app exports OTLP; the collector fans out. Ready-to-run collector + Jaeger + Prometheus + Grafana compose is in `references/otel-collector-compose.md`.
- **Jaeger / Tempo** - accept OTLP directly; point `OTEL_EXPORTER_OTLP_ENDPOINT` at them or at the collector.
- **Prometheus** - either the collector's `prometheus` exporter, or the SDK's Prometheus exporter/scrape endpoint in-process.
- **Vendors** - OTLP endpoint + auth header via `OTEL_EXPORTER_OTLP_HEADERS`. Never commit the key.

## Production defaults (apply unless told otherwise)

- **Sampling:** don't trace 100% in prod. Parent-based + ratio (start 10-20%), or tail sampling at the collector to keep all errors and slow requests.
- **Resource attributes:** always `service.name`, `service.version`, `deployment.environment`. Without them telemetry is unusable across services.
- **Record exceptions** on server spans and set error status so failures attach to traces.
- **Don't put secrets/PII** in span attributes. Be careful with DB statement capture (may include parameters).
- **Propagation:** W3C `traceparent` over HTTP/gRPC is automatic with instrumented clients; for queues, inject/extract context manually (see `distributed-tracing-diagnostics`).
- **Graceful shutdown:** flush providers on exit (short-lived jobs and serverless lose spans otherwise).
- **Batch exporters** in prod, never simple/synchronous ones.

## Output format

```
## What I'm setting up
<stack detected, app type, backend, signals>

## Dependencies
<dotnet add package / npm i / pip install / Maven-Gradle coordinates or agent jar / go get>

## Wiring
<startup code for the detected stack>

## Custom instrumentation example
<span + metric relevant to their domain>

## Run it
<env vars / compose / how to see the data>
```

Pairs with `structured-logging-setup` (log/trace correlation), `distributed-tracing-diagnostics`, and `metrics-dashboard-generator`.
