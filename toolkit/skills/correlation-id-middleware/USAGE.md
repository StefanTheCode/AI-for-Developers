# How to use - Correlation-ID Middleware

**What it is.** Adds correlation-ID middleware to your HTTP service so every request has an id that appears in every log line, is echoed to the caller, and propagates to downstream HTTP calls and messages. Works for ASP.NET Core, Express/Fastify/NestJS, FastAPI/Django/Flask, Spring Boot (MVC and WebFlux), and Go (net/http, chi, gin, echo).

**When to reach for it.** Tracing a request through logs across services, especially before full distributed tracing is in place, or when support needs an id customers can quote.

**How to use it.** Point it at your project or services (path or GitHub URL). Example prompts:
- "Add a correlation id to my ASP.NET Core requests and Serilog logs."
- "Add X-Request-ID to my Express app and pass it to axios calls."
- "Correlation id for FastAPI with structlog and httpx."
- "Put a correlation id in the MDC for my Spring Boot service, including @Async tasks."
- "Propagate the correlation id through Kafka messages in this Go service."

**What you get.** The middleware/filter (accept inbound id, else reuse trace id, else generate), log enrichment, response echo, outbound HTTP propagation, messaging publish/consume guidance, and where to register it.

**Get the best out of it.** Let it reuse the W3C trace id when tracing is present so it complements (not competes with) tracing. Accept an inbound id from your gateway first. Pick one header name for the whole system. Pairs with `structured-logging-setup` and `distributed-tracing-diagnostics`.

**Won't do.** It won't replace distributed tracing - it's the log-grep companion to it.
