# How to use - OpenTelemetry Setup

**What it is.** A skill that wires up OpenTelemetry observability - traces, metrics, and logs - in a service written in .NET, Node.js/TypeScript, Python, Java/Kotlin, or Go, exported to whatever backend you use (OTLP collector, Jaeger, Tempo, Prometheus/Grafana, Azure Monitor, or an OTLP-compatible vendor). It detects your stack, then generates production-shaped wiring, custom span/metric examples around your own domain, and a ready-to-run local collector stack.

**When to reach for it.** Adding observability to a service, setting up distributed tracing across microservices, wiring metrics to Prometheus, or troubleshooting "I can't see what my app is doing in production".

**How to use it.** Point it at your project (a path, repo, or GitHub URL). It reads the manifest (`*.csproj`, `package.json`, `pyproject.toml`, `pom.xml`/`build.gradle`, `go.mod`) and any existing OTel wiring first, and extends what's there rather than starting over. Tell it the **app type** (API / worker / CLI / library) and the **backend** if it can't infer them. Example prompts:
- "Add OpenTelemetry tracing and metrics to my ASP.NET Core API, exporting to Jaeger + Prometheus."
- "Instrument this Express/NestJS service with OTel and send it to our collector."
- "Add tracing to my FastAPI app with `opentelemetry-instrument` - we run gunicorn."
- "Set up the OpenTelemetry Java agent for this Spring Boot service in Docker."
- "Wire OTel into this Go HTTP service with otelhttp and propagate context to downstream calls."
- "I want a local observability stack to test traces - give me the compose file too."

**What you get.** Detected stack and plan, the exact dependencies to add, the startup wiring, one custom span and one custom metric for your domain, and env vars/compose to see the data.

**How to get the best out of it.**
- **Name your service and domain.** It generates custom spans/metrics around *your* operations (e.g. `PlaceOrder`, `orders.placed`) instead of generic examples.
- **Ask for the local stack** (`references/otel-collector-compose.md`) to see traces in Jaeger before touching production.
- **Mention scale.** It defaults to safe production settings (sampling, resource attributes, batch exporters) - tell it your traffic and it tunes the sampling ratio.
- **Polyglot system?** Point it at each service; the env-var config (`OTEL_*`) is the same everywhere, so traces join up across languages.
- Pairs with `structured-logging-setup`, `distributed-tracing-diagnostics`, and `metrics-dashboard-generator`.

**What it won't do.** It won't deploy or configure your real backend credentials - it shows you the wiring and env vars; you set the secrets. Sampling ratios are starting points, not tuned to your traffic.
