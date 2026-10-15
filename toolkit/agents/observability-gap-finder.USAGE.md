# How to use - Observability Gap Finder (agent)

**What it is.** An agent that audits a service in any stack - **.NET, Node.js/TypeScript, Python, Java/Kotlin (Spring), Go** - across traces, metrics, logs, health checks, and correlation, then gives a prioritized plan to make it properly observable. OpenTelemetry is the baseline; it also knows the stack tooling (Serilog, pino, winston, structlog, Logback/SLF4J, zap, slog, Micrometer, Actuator, prom-client).

**When to reach for it.** "Can I actually debug this in production", a pre-prod readiness check, or finding what telemetry is missing.

**How to use it.** Point it at the service repo (path or GitHub URL). Example prompts:
- "Review this ASP.NET Core service's observability."
- "Can we operate and debug this NestJS service in production?"
- "What telemetry is this FastAPI app missing?"
- "Are traces propagated across our Spring Boot services and Kafka?"
- "Check the logging and tracing setup in this Go service."

**Get the best out of it.** Give it the whole service (and related services for cross-service tracing gaps), including deployment and collector config. It hands fixes to `opentelemetry-setup`, `structured-logging-setup`, `healthchecks-setup`, `correlation-id-middleware`, `metrics-dashboard-generator`, and `distributed-tracing-diagnostics`.

**Won't do.** It reviews wiring, not live dashboards - confirm signals flow in your backend after applying fixes.
