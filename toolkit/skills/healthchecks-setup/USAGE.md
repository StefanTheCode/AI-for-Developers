# How to use - Health Checks Setup

**What it is.** Adds health checks with a proper liveness/readiness (and startup) split and dependency checks (DB, cache, broker, downstream APIs) for Kubernetes, ECS, and load balancers. Uses the idiomatic tool per stack: ASP.NET Core health checks, Spring Boot Actuator, NestJS Terminus or plain Express/Fastify routes, FastAPI/Django, Go net/http or gRPC health.

**When to reach for it.** Adding `/health` endpoints, k8s probes, graceful shutdown, or fixing pods that restart on dependency blips.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Add liveness and readiness health checks to my ASP.NET Core API."
- "Wire Postgres and Redis into readiness for my NestJS service."
- "Set up Actuator probes for Kubernetes in this Spring Boot app."
- "Add /healthz and /readyz to my Go service with graceful shutdown."
- "Why does a DB outage restart my pods?" (liveness depends on the DB - it fixes the split.)

**What you get.** Current-state summary, liveness/readiness (+startup) endpoints with dependency checks, orchestrator config (k8s probes, compose healthcheck), and notes on what was kept out of liveness and why.

**Get the best out of it.** Let it read your manifest to add the right dependency checks. Keep liveness dependency-free - that's the key lesson. Tell it which dependencies are critical vs. optional. Pairs with `microservice-template-generator`.

**Won't do.** It won't expose detailed health publicly without auth - that leaks topology.
