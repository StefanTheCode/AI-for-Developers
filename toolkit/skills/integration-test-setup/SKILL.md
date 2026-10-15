---
name: integration-test-setup
description: Set up integration tests for a web API/service in any stack, running the real app pipeline against a real database via Testcontainers - ASP.NET Core (WebApplicationFactory), Node (supertest with Express/Fastify/NestJS), Python (FastAPI TestClient/httpx, Django test client, testcontainers-python), Java/Kotlin (Spring Boot @SpringBootTest, MockMvc/WebTestClient, @ServiceConnection), Go (httptest, testcontainers-go). Use this skill whenever the user wants integration tests, end-to-end API tests, tests that hit a real database, WebApplicationFactory or Testcontainers setup, test fixtures with a real Postgres/SQL Server/MySQL/Redis, resetting DB state between tests (Respawn, truncation, transaction rollback), or asks "how do I integration-test my API / test against a real database". Always use this skill for integration-testing requests; it produces a working test harness and a sample passing test.
category: Testing
---

# Integration Test Setup

Stand up real integration tests for an API: boot the app in-process (or as close as the stack allows), swap in a real database from Testcontainers, reset state between tests, and ship a sample test that exercises the full HTTP pipeline.

## Input - point it at your project

Point this at the API project or repo, not a pasted file - inspect what exists first:

- **A project or repo root** - find the app entry point, the routes, and any existing integration-test setup.
- **A GitHub URL** - clone first, then inspect: `git clone --depth 1 <url> /tmp/repo && cd /tmp/repo`

If an integration-test setup already exists, **review and extend it** (flag schema created by "create all" instead of real migrations, unpinned images, a container per test, missing state reset, order-dependent tests) instead of scaffolding from scratch. Pick a representative endpoint for the first test.

## Step 1 - detect the stack, web framework, and database

| Manifest | Stack | App host for tests | Testcontainers package |
|---|---|---|---|
| `*.csproj` | ASP.NET Core | `WebApplicationFactory<Program>` | `Testcontainers.PostgreSql` / `.MsSql` / `.MySql` / `.Redis` |
| `package.json` | Express / Fastify / NestJS | `supertest` on the app instance / `fastify.inject` / `Test.createTestingModule` | `@testcontainers/postgresql` etc. |
| `pyproject.toml`, `requirements*.txt` | FastAPI / Django / Flask | `TestClient` or `httpx.AsyncClient` / Django `Client` / `app.test_client()` | `testcontainers` (`testcontainers.postgres`) |
| `pom.xml`, `build.gradle(.kts)` | Spring Boot | `@SpringBootTest` + `MockMvc` / `WebTestClient` / `TestRestTemplate` | `org.testcontainers:postgresql` + `junit-jupiter` |
| `go.mod` | net/http / chi / gin / echo | `httptest.NewServer(handler)` | `github.com/testcontainers/testcontainers-go/modules/postgres` |

Find the entry point and routes, e.g.:
```
# .NET
find <target> -name Program.cs | grep -vi "/obj/\|/bin/"
grep -rn "MapGet\|MapPost\|: ControllerBase\|ICarterModule" --include=*.cs <target> | head
# Node
grep -rnE "express\(\)|Fastify\(|NestFactory|app\.(get|post)\(" --include=*.{ts,js} <target> | grep -v node_modules | head
# Python
grep -rnE "FastAPI\(|urlpatterns|Flask\(" --include=*.py <target> | head
# Java / Kotlin
grep -rn "@RestController\|@SpringBootApplication" <target>/src | head
# Go
grep -rnE "http\.HandleFunc|chi\.NewRouter|gin\.(New|Default)|echo\.New" --include=*.go <target> | head
```
Infer the database from the driver/ORM in the manifest; default to **PostgreSQL** only if nothing points elsewhere.

## Universal setup checklist

1. **Real engine, not a fake.** In-memory providers and SQLite stand-ins don't enforce the same constraints, types, or SQL. Use the production engine in a container, with a pinned image tag (`postgres:16-alpine`, not `latest`).
2. **Run the real migrations** (EF migrations, Prisma/Knex/TypeORM migrations, Alembic, Django `migrate`, Flyway/Liquibase, goose/golang-migrate/atlas) - not "create all tables from models".
3. **Boot the real app pipeline** - routing, DI, middleware, auth, serialization, validation - and override only the connection string and true externals (third-party HTTP, email, payments).
4. **One container per test run/suite, not per test.** Start once, share, reset state.
5. **Reset state between tests:** table truncation/Respawn, or wrap each test in a rolled-back transaction where the stack supports it (not when the app commits on its own connection).
6. **Independent tests** - unique data per test, no reliance on execution order.
7. **Auth in tests** - a test auth handler/scheme or a real token from a test issuer; never disable auth globally.
8. **CI:** needs a Docker-compatible runtime. Note this for the pipeline.

## C# / ASP.NET Core

```
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers.PostgreSql   # or Testcontainers.MsSql
dotnet add package xunit
dotnet add package FluentAssertions
dotnet add package Respawn                      # reset DB state between tests
```

```csharp
public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the app's registered DbContext and point it at the container
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(_db.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ctx.Database.MigrateAsync();
    }

    public new async Task DisposeAsync() => await _db.DisposeAsync();
}

[CollectionDefinition(nameof(IntegrationCollection))]
public class IntegrationCollection : ICollectionFixture<IntegrationTestFactory> { }

[Collection(nameof(IntegrationCollection))]
public class OrdersApiTests
{
    private readonly HttpClient _client;
    public OrdersApiTests(IntegrationTestFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task CreateOrder_ReturnsCreated_AndPersists()
    {
        var request = new { customerId = Guid.NewGuid(), lines = new[] { new { sku = "SKU1", qty = 2 } } };

        var response = await _client.PostAsJsonAsync("/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        (await _client.GetAsync($"/orders/{id}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```
- `Program` must be reachable - add `public partial class Program { }` at the bottom of `Program.cs` with top-level statements.
- Use `MigrateAsync`, not `EnsureCreatedAsync` (the latter skips migrations).
- State reset with Respawn after migrating: `_respawner = await Respawner.CreateAsync(conn, new RespawnerOptions { DbAdapter = DbAdapter.Postgres });` then `await _respawner.ResetAsync(conn)` in a per-test hook. Respawn needs an open `DbConnection` for Postgres.
- The EF Core in-memory provider doesn't enforce constraints or translate SQL - don't use it for integration tests.

## TypeScript / Node

- Export the app separately from `listen()` so tests can import it; drive it with `supertest` (Express/NestJS via `app.getHttpServer()`) or `app.inject()` (Fastify).
- Start the container in a global setup (Vitest `globalSetup` / Jest `globalSetup`), set `DATABASE_URL`, run migrations (`prisma migrate deploy`, `knex migrate:latest`, TypeORM `runMigrations()`), then import the app.
- Reset with `TRUNCATE ... RESTART IDENTITY CASCADE` in `beforeEach`. Run test files serially (or one schema/DB per worker) when they share a database.

## Python

- FastAPI: `TestClient(app)` (sync) or `httpx.AsyncClient` with `ASGITransport(app=app)`; override settings or the DB dependency via `app.dependency_overrides`.
- Django: `pytest-django` with the `django_db` marker or `TestCase`/`TransactionTestCase`; point `DATABASES` at the container.
- Session-scoped fixture: `with PostgresContainer("postgres:16-alpine") as pg:` -> `pg.get_connection_url()`, run `alembic upgrade head` (or Django migrations), yield. Per-test: transaction rollback (SQLAlchemy connection + nested transaction) or truncation.

## Java / Kotlin (Spring Boot)

- `@SpringBootTest` + `@AutoConfigureMockMvc` (or `webEnvironment = RANDOM_PORT` with `WebTestClient`/`TestRestTemplate`), `@Testcontainers`, a `static @Container PostgreSQLContainer<?>`.
- Wire the connection with `@ServiceConnection` (Spring Boot 3.1+) or `@DynamicPropertySource`. Flyway/Liquibase run on startup automatically.
- Share one container across test classes (abstract base class with a static container) so Spring's context cache stays warm.
- `@Transactional` on a test rolls back only for MockMvc-style in-thread calls - not for `RANDOM_PORT` HTTP calls; use truncation there.

## Go

- Build the router/handler from a constructor that takes its dependencies, then `srv := httptest.NewServer(handler)`; `defer srv.Close()`.
- Start Postgres once in `TestMain` with the `testcontainers-go` postgres module, run migrations (goose / golang-migrate / atlas), pass the DSN to the app.
- Gate with a build tag (`//go:build integration`) or `testing.Short()` so `go test ./...` stays fast; reset with `TRUNCATE` per test.

## Output format

```
## Detected
<stack, web framework, DB, migration tool, existing test setup (reviewed or new)>

## Packages / dependencies
<exact install commands for the stack>

## Harness
<factory / global setup / fixtures / base class with container + migrations + state reset>

## Sample test
<one passing test against a representative endpoint>

## Notes
<CI requirements (Docker), anything to verify, review findings if a setup existed>
```

## Principles

- **Test the seams that mocks can't reach:** routing, binding/validation, auth, ORM mappings, migrations, serialization.
- **Real DB beats in-memory fakes.** Testcontainers gives you the real engine.
- **Keep tests independent** - reset state rather than depending on order.
- **One container per suite**, not per test, or the suite crawls.
- Don't integration-test pure logic that a unit test already covers - integration tests are slower; spend them where they buy confidence.
