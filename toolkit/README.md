# AI for Developers Toolkit

**45 Claude Code skills and 7 specialist agents that run on your real code, in any stack.** .NET, Node/TypeScript, Python, Java/Kotlin and Go are all first-class: every skill detects your stack first, then applies the right tools and idioms. Plus a separate **[.NET Pack](../packs/dotnet)** with 4 .NET-only skills.

Part of the **[AI for Developers](https://www.skool.com/ai-for-dotnet-developers/about)** community by [Stefan Đokić - TheCodeMan](https://thecodeman.net), Microsoft MVP.

---

## Install (one marketplace, two commands)

```shell
/plugin marketplace add StefanTheCode/AI-for-Developers
/plugin install ai-dev-toolkit@thecodeman-ai-toolkit
```

.NET developers can add the .NET Pack too:

```shell
/plugin install dotnet-pack@thecodeman-ai-toolkit
```

Update anytime:

```shell
/plugin marketplace update thecodeman-ai-toolkit
```

> Coming from the old `dotnet-ai-toolkit` plugin? See **[INSTALL.md](INSTALL.md#migrating-from-dotnet-ai-toolkit)**. Manual install is there too.

---

## Skill vs. agent

**Skill** - a focused capability Claude loads automatically when your request matches it. One skill = one job done well ("optimize this query", "generate tests for this service").

**Agent** - a specialist that explores your whole codebase on its own and hands you a ranked report ("audit the security of this API", "review this PR").

---

## How to use

Install, then talk to Claude Code in plain language. It routes to the right skill or agent:

```
> This Prisma query is slow, optimize it
> This EF Core query is slow, optimize it
> Scaffold a CRUD endpoint for products in FastAPI
> Write integration tests with Testcontainers for the checkout flow
> Add OpenTelemetry to this Spring Boot service
> Review this PR
> Audit the security of this API
```

---

## Skills

### Architecture (8)

| Skill | What it does |
|---|---|
| **[api-endpoint-scaffolder](skills/api-endpoint-scaffolder)** | Scaffold HTTP API endpoints in any stack. |
| **[clean-architecture-scaffolder](skills/clean-architecture-scaffolder)** | Scaffold or restructure a backend project using Clean Architecture (layers + dependency rule) or Vertical Slice Architecture, in any stack - .NET solutions, TypeScript (NestJS or Express), Python (FastAPI), Java/Kotlin (Spring Boot), Go. |
| **[cqrs-setup](skills/cqrs-setup)** | Set up CQRS (command/query separation) in any stack. |
| **[crud-vs-event-sourcing](skills/crud-vs-event-sourcing)** | Decide whether a feature/aggregate should be built as CRUD, Event Sourcing, or an event-driven design (CRUD + outbox). |
| **[ddd-aggregate-generator](skills/ddd-aggregate-generator)** | Generate Domain-Driven Design building blocks in any language. |
| **[microservice-template-generator](skills/microservice-template-generator)** | Generate a production-shaped microservice template in any stack. |
| **[modular-monolith-generator](skills/modular-monolith-generator)** | Structure an app in any stack as a modular monolith. |
| **[result-pattern-scaffolder](skills/result-pattern-scaffolder)** | Add a Result/Error pattern (errors as values) to a codebase in any stack so expected failures stop using exceptions for control flow - Result<T> in C# (or ErrorOr / FluentResults / OneOf), discriminated unions or neverthrow in TypeScript, Ok/Err dataclasses or the returns library in Python, sealed interfaces / Vavr Either / Kotlin Result or Arrow in Java/Kotlin, and idiomatic typed/sentinel errors with errors.Is/As in Go - plus mapping domain errors to HTTP in one place. |

### Database (8)

| Skill | What it does |
|---|---|
| **[db-index-advisor](skills/db-index-advisor)** | Recommend database indexes based on how tables/entities are actually queried. |
| **[db-migration-reviewer](skills/db-migration-reviewer)** | Review database schema migrations for destructive or risky operations before they hit production. |
| **[db-resiliency-setup](skills/db-resiliency-setup)** | Add database resiliency to any stack. |
| **[orm-config-auditor](skills/orm-config-auditor)** | Audit ORM and data-layer configuration for correctness and performance pitfalls in any stack. |
| **[orm-query-optimizer](skills/orm-query-optimizer)** | Analyze and optimize ORM and database-access code in any stack. |
| **[specification-pattern-generator](skills/specification-pattern-generator)** | Implement the Specification pattern (reusable, composable, testable query criteria) in any stack. |
| **[sql-orm-converter](skills/sql-orm-converter)** | Convert raw SQL to ORM query code and ORM queries back to the SQL they generate, both directions, preserving semantics - EF Core LINQ, Prisma, Drizzle, TypeORM QueryBuilder, Knex, SQLAlchemy 2.0 select(), Django QuerySet, JPA Criteria / JPQL / Spring Data, jOOQ, and GORM. |
| **[temporal-tables-setup](skills/temporal-tables-setup)** | Set up row history and auditing in any stack. |

### Testing (6)

| Skill | What it does |
|---|---|
| **[architecture-test-generator](skills/architecture-test-generator)** | Generate architecture tests / fitness functions that fail the build when architectural rules are violated. |
| **[integration-test-setup](skills/integration-test-setup)** | Set up integration tests for a web API/service in any stack, running the real app pipeline against a real database via Testcontainers - ASP.NET Core (WebApplicationFactory), Node (supertest with Express/Fastify/NestJS), Python (FastAPI TestClient/httpx, Django test client, testcontainers-python), Java/Kotlin (Spring Boot @SpringBootTest, MockMvc/WebTestClient, @ServiceConnection), Go (httptest, testcontainers-go). |
| **[mutation-testing-setup](skills/mutation-testing-setup)** | Set up mutation testing to measure whether tests actually catch bugs, and interpret the mutation score and surviving mutants - Stryker.NET (C#/.NET), StrykerJS (TypeScript/JavaScript), mutmut (Python), PIT/pitest (Java/Kotlin), gremlins or go-mutesting (Go), cargo-mutants (Rust). |
| **[test-coverage-gap-finder](skills/test-coverage-gap-finder)** | Find the meaningful test-coverage gaps in any codebase. |
| **[test-data-builder-generator](skills/test-data-builder-generator)** | Generate test data builders, factories, and object mothers for test suites in any stack so tests create valid domain objects without brittle, repetitive setup - C#/.NET (fluent builders, Bogus, AutoFixture), TypeScript/JavaScript (fishery, @faker-js/faker), Python (factory_boy, Faker, polyfactory), Java/Kotlin (builders, Instancio, Datafaker), Go (builder/functional options, gofakeit). |
| **[unit-test-generator](skills/unit-test-generator)** | Generate complete, runnable unit tests for code in any stack. |

### Performance (7)

| Skill | What it does |
|---|---|
| **[async-await-auditor](skills/async-await-auditor)** | Audit async and concurrent code in any language for correctness and performance bugs. |
| **[benchmark-setup](skills/benchmark-setup)** | Set up correct micro-benchmarks in any stack and interpret the results. |
| **[caching-strategy-setup](skills/caching-strategy-setup)** | Add the right caching to an app in any stack. |
| **[gc-pressure-auditor](skills/gc-pressure-auditor)** | Diagnose garbage-collection and memory pressure in a service on any managed runtime and recommend fixes. |
| **[hotpath-profiler-assistant](skills/hotpath-profiler-assistant)** | Guide profiling an app in any stack to find the real hot paths before optimizing. |
| **[memory-allocation-analyzer](skills/memory-allocation-analyzer)** | Find unnecessary heap allocations on hot paths in any stack and rewrite them to reduce GC/memory pressure. |
| **[serialization-optimizer](skills/serialization-optimizer)** | Optimize JSON serialization in any stack. |

### Security (4)

| Skill | What it does |
|---|---|
| **[authorization-setup](skills/authorization-setup)** | Design API authorization in any stack. |
| **[dependency-vuln-scanner](skills/dependency-vuln-scanner)** | Scan a project's dependencies in any ecosystem for known vulnerabilities (CVEs/GHSA), deprecated or abandoned packages, and transitive risks, then recommend the minimal safe upgrades. |
| **[jwt-auth-setup](skills/jwt-auth-setup)** | Set up JWT authentication in any stack. |
| **[secrets-config-auditor](skills/secrets-config-auditor)** | Audit a codebase in any stack for hardcoded secrets and unsafe configuration. |

### Observability (6)

| Skill | What it does |
|---|---|
| **[correlation-id-middleware](skills/correlation-id-middleware)** | Add correlation-ID (request-ID) middleware to an HTTP service in any stack. |
| **[distributed-tracing-diagnostics](skills/distributed-tracing-diagnostics)** | Diagnose and fix broken distributed traces in any stack (.NET, Node.js, Python, Java/Kotlin, Go). |
| **[healthchecks-setup](skills/healthchecks-setup)** | Add health checks with a correct liveness/readiness (and startup) split plus dependency checks (database, cache, message broker, downstream APIs) for Kubernetes, ECS, and load balancers - in any stack. |
| **[metrics-dashboard-generator](skills/metrics-dashboard-generator)** | Generate Prometheus scrape config and a ready-to-import Grafana dashboard JSON for a service in any stack. |
| **[opentelemetry-setup](skills/opentelemetry-setup)** | Add or fix OpenTelemetry observability (traces, metrics, logs) in a service written in any stack. |
| **[structured-logging-setup](skills/structured-logging-setup)** | Set up structured (JSON) logging in any stack. |

### DevOps (4)

| Skill | What it does |
|---|---|
| **[cicd-pipeline-generator](skills/cicd-pipeline-generator)** | Generate a CI/CD pipeline for an app in any stack. |
| **[dependency-analyzer](skills/dependency-analyzer)** | Analyze a project's or monorepo's dependencies in any ecosystem. |
| **[dockerfile-generator](skills/dockerfile-generator)** | Generate an optimized multi-stage Dockerfile and .dockerignore for an app in any stack. |
| **[typed-config-generator](skills/typed-config-generator)** | Generate strongly-typed, validated-at-startup configuration in any stack. |

### AI Tooling (1)

| Skill | What it does |
|---|---|
| **[codebase-health-dashboard](skills/codebase-health-dashboard)** | Run the whole toolkit against a repo in any stack (.NET, Node.js/TypeScript, Python, Java/Kotlin, Go, Rust, or a polyglot monorepo) and produce a single scored "health dashboard" - an overall score plus per-category scores (security, architecture, database/ORM, performance, observability, testing, dependencies, code quality), each with findings and the top fixes. |

### Meta / Tooling (1)

| Skill | What it does |
|---|---|
| **[toolkit-dashboard](skills/toolkit-dashboard)** | Build, refresh, and OPEN the TheCodeMan AI for Developers Toolkit dashboard. |

---

## Agents (7)

| Agent | What it does |
|---|---|
| **[api-security-auditor](agents/api-security-auditor.md)** | Audits a backend or web API codebase in any stack (ASP.NET Core, Node.js Express/NestJS, Python FastAPI/Django, Java/Kotlin Spring Boot, Go net/http/Gin, and others) against the OWASP Top 10 and OWASP API Security Top 10 - broken access control and object-level authorization (IDOR/BOLA), authentication and JWT flaws, injection, secrets in source, insecure configuration, CORS, mass assignment, SSRF, missing rate limits, and vulnerable dependencies. |
| **[architecture-reviewer](agents/architecture-reviewer.md)** | Reviews a backend codebase or repository in any stack (.NET, Node.js/TypeScript, Python, Java/Kotlin Spring, Go, and others) and produces a structured architecture report - layering and dependency-rule violations, module boundaries, coupling, CQRS/handler hygiene, ORM and persistence boundary leaks, testability, and concrete prioritized fixes. |
| **[code-reviewer](agents/code-reviewer.md)** | Performs a PR-level code review of changes in any language (C#/.NET, TypeScript/JavaScript, Python, Java/Kotlin, Go, and others) - correctness, readability, naming, error handling, async/concurrency, data-access and performance pitfalls, API design, test coverage, and idiomatic use of the language. |
| **[db-performance-auditor](agents/db-performance-auditor.md)** | Audits a codebase's data-access layer end to end for performance in any stack - ORM query patterns (N+1, projections, tracking, eager/lazy loading), indexing gaps, ORM and connection-pool configuration, retry/resiliency, and bulk-operation opportunities - across EF Core, Dapper, Prisma, TypeORM, Drizzle, SQLAlchemy, Django ORM, Hibernate/JPA, Spring Data, GORM, and raw SQL. |
| **[framework-upgrade-agent](agents/framework-upgrade-agent.md)** | Plans and executes a runtime or framework major-version upgrade across a codebase in any stack - .NET (target framework bumps, .NET Framework to modern .NET, Upgrade Assistant), Node.js LTS and major dependency bumps (Express, NestJS, Next.js, React, TypeScript), Python versions (pyupgrade, Django/FastAPI/Pydantic majors), Java LTS and Spring Boot (OpenRewrite, javax to jakarta), and Go versions - with breaking-change remediation, deprecated-API replacement, dependency updates, and a per-stage verification plan. |
| **[legacy-modernization-assistant](agents/legacy-modernization-assistant.md)** | Assesses a legacy codebase in any stack and produces a pragmatic, incremental modernization roadmap - strangler fig over big-bang rewrite, monolith to modular, toward testability, dependency injection, async, typed code, and current platform versions - prioritized by risk and value. |
| **[observability-gap-finder](agents/observability-gap-finder.md)** | Audits a backend service in any stack (.NET, Node.js/TypeScript, Python, Java/Kotlin Spring, Go) for observability gaps - missing or incomplete distributed tracing, metrics, structured logging, health checks, and correlation - and produces a prioritized plan to make the service properly observable. |

---

## Also in this folder

| Folder | What it is |
|---|---|
| **[templates](templates)** | CLAUDE.md templates: a generic one for any stack plus filled examples for .NET, TypeScript and Python. |
| **[dashboards](dashboards)** | The toolkit dashboard (every skill and agent on one page) and a sample codebase health report. |
| **[tools](tools)** | CLAUDE.md generator (a single HTML page). |

---

## Get the most out of it

Every skill has a video or written lesson inside the **[AI for Developers community](https://www.skool.com/ai-for-dotnet-developers/about)** on Skool (7-day free trial): when to use it, the exact prompts, what good output looks like, plus live office hours and new skills every week.

📬 Free: [weekly newsletter on .NET, architecture & AI](https://thecodeman.net) (21,000+ developers) · ▶️ [YouTube](https://www.youtube.com/@thecodeman_)

*Built by [Stefan Đokić](https://thecodeman.net) · MIT License*
