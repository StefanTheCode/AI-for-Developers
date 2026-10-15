# How to use - Microservice Template Generator

**What it is.** Scaffolds a new service with production essentials wired up front: health checks (live/ready), OpenTelemetry traces and metrics, structured JSON logging, validated typed config, graceful shutdown, Docker, and a messaging consumer/producer stub. Works for ASP.NET Core, Node (Fastify/Express/NestJS), Python (FastAPI), Java/Kotlin (Spring Boot), and Go.

**When to reach for it.** Starting a new service, or standardizing how new services look on your platform.

**How to use it.** For a new service, give it the name, responsibility, and stack. For an existing platform, point it at the repo (path or GitHub URL) to match the house template. Example prompts:
- "Scaffold an orders microservice in .NET with NATS messaging."
- "New service matching our existing services' setup."
- "Fastify service template with pino, OpenTelemetry, and a Kafka consumer."
- "FastAPI service skeleton with structlog, pydantic-settings, and health endpoints."
- "Spring Boot service with Actuator probes, OTLP tracing, and a RabbitMQ listener."
- "Go service with slog, graceful shutdown, and a Dockerfile."

**Get the best out of it.** Let it read an existing service so the new one is consistent. Name your broker and where telemetry goes (collector endpoint). Pair with `dockerfile-generator`, `healthchecks-setup`, `structured-logging-setup`, `opentelemetry-setup`, and `typed-config-generator` for deeper config.

**What you get.** The detected stack and template it matched, a full file tree with content (entry point, config, health, telemetry, logging, messaging stub, Dockerfile, README), a config variable table, and run-locally commands.

**Won't do.** It won't build your domain - it gives the production-ready shell; feature skills fill it in.
