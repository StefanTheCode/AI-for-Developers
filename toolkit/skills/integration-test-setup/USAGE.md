# How to use - Integration Test Setup

**What it is.** A skill that stands up real integration tests for your API in any stack: it boots the real app pipeline, swaps in a real database via Testcontainers, runs your actual migrations, resets state between tests, and adds a sample test that drives the full HTTP path. It knows ASP.NET Core (`WebApplicationFactory` + Respawn), Node (supertest / Fastify inject / NestJS testing module), Python (FastAPI `TestClient`/httpx, Django, testcontainers-python), Spring Boot (`@SpringBootTest`, `@ServiceConnection`), and Go (`httptest` + testcontainers-go).

**When to reach for it.** When unit tests aren't enough - you need to test routing, DI, middleware, auth, ORM mappings, serialization, and migrations against a real database.

**How to use it.** Point it at your API project (a path or GitHub URL). It finds the entry point, routes, database, and any existing test setup, and reviews/extends rather than duplicating. Example prompts:
- "Set up integration tests for my ASP.NET Core API against a real Postgres."
- "Add supertest + Testcontainers integration tests for my Express app in `api/`."
- "Integration tests for my FastAPI service with a real Postgres and Alembic migrations."
- "Spring Boot integration tests for the orders controller with Testcontainers MySQL."
- "Set up httptest-based integration tests for this Go service: https://github.com/me/orders"

**How to get the best out of it.**
- **Have Docker running** locally and in CI - Testcontainers spins up a real DB, which is the whole point.
- **Tell it your migration tool** if it isn't obvious (EF migrations, Prisma, Alembic, Flyway, goose).
- **Ask for the state-reset hook** (Respawn, truncation, or transaction rollback) so tests stay independent without restarting containers.
- **Give it a representative endpoint** and it produces a test you can copy for the rest.

**What you get.** The detected setup, install commands, the test harness (factory/fixtures/base class with container + migrations + reset), one passing sample test, and CI notes.

**What it won't do.** It won't replace unit tests - don't integration-test pure logic a fast unit test already covers. It assumes a container runtime is available in your test environment/CI.
