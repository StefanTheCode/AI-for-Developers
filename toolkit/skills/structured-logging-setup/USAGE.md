# How to use - Structured Logging Setup

**What it is.** Wires structured JSON logging into your service - context enrichment, one line per request, levels, redaction, a sink, and trace correlation. It detects your stack and uses the idiomatic library: Serilog (.NET), pino or winston (Node.js), structlog (Python), SLF4J + Logback (Java/Kotlin), slog/zap/zerolog (Go).

**When to reach for it.** Moving off `Console.WriteLine` / `console.log` / `print` / default logging, or you need logs you can actually query and join to traces.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Set up Serilog with JSON logs and a Seq sink."
- "Add pino and pino-http to my Express API, redact auth headers."
- "Configure structlog for my FastAPI app with a request id on every line."
- "Make my Spring Boot service log JSON and include the trace id."
- "Switch this Go service to slog with request logging middleware."
- "Fix my logging so it's actually structured."

**What you get.** Current-state summary, install commands, logger wiring (request logging, context enrichment, redaction), good-vs-bad log call examples in your code style, and level/sink config per environment.

**Get the best out of it.** Let it detect OTel and wire log/trace correlation. Ask for one-line-per-request logging to cut default framework noise. If you already use a library (winston, zap, Log4j2), it improves that setup instead of migrating you. Pairs with `opentelemetry-setup` and `correlation-id-middleware`.

**Won't do.** It won't decide your PII policy - it redacts the obvious secrets and flags the rest; you set what's allowed.
