---
name: metrics-dashboard-generator
description: Generate Prometheus scrape config and a ready-to-import Grafana dashboard JSON for a service in any stack - RED method panels (rate, errors, duration) plus runtime health (GC, heap, threads/goroutines/event loop) and the service's custom metrics. Works with OpenTelemetry metrics (.NET, Node.js, Python, Java, Go), Micrometer/Spring Boot Actuator, prom-client, prometheus_client, and client_golang. Use this skill whenever the user wants a Grafana dashboard, Prometheus config, metrics panels, RED/USE dashboards, PromQL for latency/error rate, to visualize service metrics, or asks "how do I dashboard my metrics / Grafana for my service". Always use this skill for metrics-dashboard requests; it emits ready-to-import config.
category: Observability
version: 1.0.0
---

# Metrics Dashboard Generator

Produce ready-to-import Prometheus scrape config and a Grafana dashboard JSON for a service - built around the RED method (Rate, Errors, Duration) plus runtime health and your custom metrics, using the metric names your stack actually emits.

## Input - point it at your project
Works on a target: a **project** or **GitHub URL**. Find how metrics are exposed and which custom instruments exist; note custom metric names to add panels for them. If a running `/metrics` endpoint is reachable, read it - the real names beat any convention.

## Step 1 - detect the stack and metrics library

| Manifest | Stack | Grep for | Typical exposure |
|---|---|---|---|
| `*.csproj` | .NET | `AddPrometheusExporter\|MapPrometheusScrapingEndpoint\|Meter(\|CreateCounter\|CreateHistogram\|AddOpenTelemetry` | OTel Prometheus exporter, `/metrics` |
| `package.json` | Node / TS | `prom-client\|collectDefaultMetrics\|@opentelemetry/exporter-prometheus\|createCounter\|createHistogram` | prom-client or OTel exporter, `/metrics` |
| `pyproject.toml`, `requirements*.txt` | Python | `prometheus_client\|prometheus-fastapi-instrumentator\|django-prometheus\|create_counter` | `/metrics` |
| `pom.xml`, `build.gradle(.kts)` | Java / Kotlin | `micrometer-registry-prometheus\|MeterRegistry\|@Timed\|Counter.builder` | `/actuator/prometheus` |
| `go.mod` | Go | `prometheus/client_golang\|promhttp\|promauto\|otel/exporters/prometheus` | `/metrics` |

## Prometheus scrape (all stacks)
```yaml
scrape_configs:
  - job_name: 'orders-service'
    metrics_path: /metrics            # Spring Boot: /actuator/prometheus
    static_configs:
      - targets: ['orders-service:8080']
```
On Kubernetes prefer a `ServiceMonitor`/`PodMonitor` (Prometheus Operator) or the scrape annotations your cluster uses. If metrics go OTLP -> collector, scrape the collector's Prometheus exporter instead.

## Metric names per stack (verify against `/metrics`)

| Signal | OTel HTTP semconv (.NET 8+, recent Node/Python/Java/Go instrumentation) | Spring Boot / Micrometer | Node prom-client | Python prometheus_client | Go client_golang |
|---|---|---|---|---|---|
| Request count / latency | `http_server_request_duration_seconds_{count,bucket}` | `http_server_requests_seconds_{count,bucket}` | your own histogram (no default HTTP metric) | your own, or `http_request_duration_seconds` from prometheus-fastapi-instrumentator | your own via `promhttp.InstrumentHandler*` |
| Route / status labels | `http_route`, `http_response_status_code`, `http_request_method` | `uri`, `status`, `method`, `outcome` | as you define | `handler`, `status`, `method` (instrumentator) | as you define |
| Runtime | .NET runtime instrumentation (GC collections, heap, thread pool queue, working set) | `jvm_memory_used_bytes`, `jvm_gc_pause_seconds`, `jvm_threads_live_threads` | `nodejs_eventloop_lag_seconds`, `nodejs_heap_size_used_bytes`, `process_cpu_seconds_total` | `process_resident_memory_bytes`, `python_gc_objects_collected_total` | `go_goroutines`, `go_memstats_heap_alloc_bytes`, `go_gc_duration_seconds` |

Notes:
- Older OTel instrumentations emitted `http.server.duration` in **milliseconds**; Micrometer histograms need `management.metrics.distribution.percentiles-histogram.http.server.requests=true` to emit `_bucket` series.
- .NET runtime metric names changed between the `OpenTelemetry.Instrumentation.Runtime` package and the built-in .NET 9 runtime meter - read the real names before writing panels.
- OTel -> Prometheus naming: dots become underscores, units are appended (`_seconds`, `_bytes`), counters get `_total`.

## Grafana dashboard (panels generated)
- **Rate** - requests/sec by route: `sum by (http_route) (rate(http_server_request_duration_seconds_count[1m]))`.
- **Errors** - 5xx ratio: `sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m])) / sum(rate(http_server_request_duration_seconds_count[5m]))`.
- **Duration** - p50/p95/p99: `histogram_quantile(0.95, sum by (le, http_route) (rate(http_server_request_duration_seconds_bucket[5m])))`.
- **Runtime (USE)** - per stack from the table: GC collections/pauses, heap, threads/thread pool queue (.NET, JVM), event loop lag (Node), goroutines (Go), CPU, working set/RSS.
- **Custom metrics** - a panel per custom counter (rate) or histogram (quantiles) you detected.
- **Variables** - `$job`/`$instance` (and `$namespace` on k8s) so one dashboard serves every instance and environment.

Swap metric and label names to the stack's row before emitting. Output is a complete dashboard `.json` (with a Prometheus datasource variable, not a hardcoded UID) to import into Grafana, plus the scrape snippet.

## Stack hints for exposing metrics
- **.NET:** `OpenTelemetry ... .WithMetrics(m => m.AddPrometheusExporter())` + `app.MapPrometheusScrapingEndpoint();` (or OTLP to a collector).
- **Node:** `prom-client` with `collectDefaultMetrics()` and a `/metrics` route returning `register.metrics()`; or the OTel Prometheus exporter.
- **Python:** `prometheus_client` (`make_asgi_app()` mounted at `/metrics` for ASGI), `prometheus-fastapi-instrumentator`, or `django-prometheus`.
- **Java:** `spring-boot-starter-actuator` + `micrometer-registry-prometheus`, expose `prometheus` in `management.endpoints.web.exposure.include`.
- **Go:** `promhttp.Handler()` on `/metrics`; wrap handlers with `promhttp.InstrumentHandlerDuration` for request histograms.

## Principles
- **RED for request services, USE for resources** - start there before vanity panels.
- Use **histogram quantiles** for latency, not averages (averages hide tail latency).
- Label by route template and status, but watch **cardinality** - never label by user id, raw path, or request id.
- Match metric names to what your exporter actually emits (verify on `/metrics`); conventions differ per stack and per instrumentation version.
- Add an SLO-style panel (error budget or p95 vs target) if the user has targets.

## Output format

```
## Detected
<stack, metrics library, endpoint, custom metrics found>

## Prometheus scrape config
<yaml>

## Grafana dashboard
<complete dashboard JSON>

## Verify
<metric/label names to confirm on /metrics, cardinality warnings>
```

## How to use it & best prompts
"Generate a Grafana dashboard for my service", "Prometheus config + RED panels", "dashboard my custom order metrics", "p95 latency panel by route for my Spring Boot app", "Grafana for a Go service using client_golang". Pairs with `opentelemetry-setup`.
