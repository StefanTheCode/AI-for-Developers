---
name: clean-architecture-scaffolder
description: Scaffold or restructure a backend project using Clean Architecture (layers + dependency rule) or Vertical Slice Architecture, in any stack - .NET solutions, TypeScript (NestJS or Express), Python (FastAPI), Java/Kotlin (Spring Boot), Go. Use this skill whenever the user wants to start a new backend project, structure or restructure an existing one, add a new feature/use-case/slice consistently, set up layers (domain, application, infrastructure, API), add CQRS, enforce the dependency rule, or asks "how should I structure my project", "scaffold a clean architecture project", "set up vertical slices". Always use this skill for project-structure and scaffolding requests; it generates a consistent layout, wiring, a working sample feature, and an architecture guardrail test rather than prose advice.
---

# Clean / Vertical Slice Architecture Scaffolder

Generate a consistent, opinionated backend structure - Clean Architecture (layered) or Vertical Slice Architecture (feature-first) - with dependencies wired correctly and one working sample feature so the pattern is obvious. The concepts are universal; the folder/project layout follows each stack's idioms.

## Input - point it at your project

For a new project, take the name and stack. For an **existing** project (path or GitHub URL - clone with `git clone --depth 1 <url> /tmp/repo`), detect the stack and current architecture first, then scaffold a new slice/feature that matches the house style instead of imposing one.

Detect:
```
ls <target>   # *.sln/*.csproj | package.json | pyproject.toml | pom.xml/build.gradle* | go.mod
find <target> -type d \( -iname features -o -iname domain -o -iname application -o -iname infrastructure -o -iname internal \) -not -path "*/node_modules/*"
```
When adding to an existing codebase, **read one existing feature end to end first** and mirror its conventions (endpoint style, validation, error/result type, DI, tests).

## Universal concepts

- **Clean Architecture** - layers: **Domain** (entities, value objects, domain rules; no framework or I/O), **Application** (use cases, ports/interfaces, DTOs), **Infrastructure** (DB, messaging, external APIs - implements the ports), **API/Presentation** (HTTP, DI composition root). Good for larger teams, long-lived domains, swappable infrastructure.
- **The dependency rule** - dependencies point inward. Domain depends on nothing; Application depends on Domain; Infrastructure and API depend inward. Application defines the interface, Infrastructure implements it.
- **Vertical Slice Architecture** - organize by feature; each slice owns its request, handler, validation, endpoint, and data access. Shared concerns (DB setup, error handling, auth) live in a small `common`/`shared` area. Good for fast-moving teams and CRUD-heavy apps; fewer cross-layer jumps.
- **CQRS (optional)** - separate command (write) and query (read) handlers. Useful with either style; a mediator library is optional, not required.
- **Guardrail test** - enforce the dependency rule with a test so the structure can't silently rot.

**Choosing:** match the style to the team and the problem, not the trend. Don't turn a CRUD app into four layers. If unclear, ask about team size, domain complexity, and expected lifetime, then recommend one.

## Workflow

1. Detect stack and existing style (or confirm style, name, runtime version, and CQRS yes/no for new projects).
2. Produce **shell commands** to create the structure and **contents of the key files**. The user should be able to run them and have a building, running project.
3. Generate **one working sample feature** (Create Order) in the chosen style.
4. Add the **guardrail test** for the stack.

## C# / .NET

Clean Architecture:
```
src/
├── Acme.Domain/            # entities, value objects, domain events - NO dependencies
├── Acme.Application/       # use cases (CQRS handlers), interfaces, DTOs - depends on Domain
├── Acme.Infrastructure/    # EF Core, external services - depends on Application
└── Acme.Api/               # endpoints, DI, middleware - depends on Application + Infrastructure
tests/  Acme.Domain.Tests/  Acme.Application.Tests/  Acme.Api.Tests/  Acme.ArchitectureTests/
```
```bash
dotnet new sln -n Acme
dotnet new classlib -n Acme.Domain         -o src/Acme.Domain
dotnet new classlib -n Acme.Application    -o src/Acme.Application
dotnet new classlib -n Acme.Infrastructure -o src/Acme.Infrastructure
dotnet new webapi   -n Acme.Api            -o src/Acme.Api
dotnet sln add src/**/*.csproj
dotnet add src/Acme.Application/Acme.Application.csproj       reference src/Acme.Domain/Acme.Domain.csproj
dotnet add src/Acme.Infrastructure/Acme.Infrastructure.csproj reference src/Acme.Application/Acme.Application.csproj
dotnet add src/Acme.Api/Acme.Api.csproj reference src/Acme.Application/Acme.Application.csproj src/Acme.Infrastructure/Acme.Infrastructure.csproj
```
Vertical Slice: a single `Acme.Api` project with `Features/Orders/CreateOrder.cs` (request, handler, validator, endpoint in one file), `Features/Orders/GetOrder.cs`, and `Common/` for the `DbContext`, pipeline behaviors, and the `Result` type.

Sample feature:
```csharp
public record CreateOrderCommand(Guid CustomerId, List<OrderLine> Lines) : IRequest<Result<Guid>>;

public class CreateOrderHandler(IOrderRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = Order.Create(cmd.CustomerId, cmd.Lines);
        if (order.IsFailure) return Result.Failure<Guid>(order.Error);
        await repo.AddAsync(order.Value, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(order.Value.Id);
    }
}

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
    }
}

app.MapPost("/orders", async (CreateOrderCommand cmd, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(cmd, ct);
    return result.IsSuccess
        ? Results.Created($"/orders/{result.Value}", result.Value)
        : Results.BadRequest(result.Error);
});
```
Building blocks to offer (not force): MediatR or a hand-rolled dispatcher (check MediatR's current license terms for commercial use), FluentValidation as a pipeline behavior, a Result type, and in Vertical Slice use the `DbContext` directly instead of repository wrappers. Keep Domain free of EF attributes (use Fluent configuration).

Guardrail (NetArchTest):
```csharp
[Fact]
public void Domain_should_not_depend_on_other_layers()
{
    var result = Types.InAssembly(typeof(Order).Assembly)
        .Should().NotHaveDependencyOnAny("Acme.Application", "Acme.Infrastructure", "Acme.Api")
        .GetResult();
    Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
}
```

## TypeScript - NestJS or Express

NestJS (feature modules = natural vertical slices; layers inside each module for Clean):
```
src/
├── orders/
│   ├── domain/            order.entity.ts, order.ts (rules, no Nest imports)
│   ├── application/       create-order.handler.ts, order.repository.ts (interface / abstract class)
│   ├── infrastructure/    prisma-order.repository.ts (or TypeORM)
│   ├── api/               orders.controller.ts, create-order.dto.ts (class-validator)
│   └── orders.module.ts   binds { provide: OrderRepository, useClass: PrismaOrderRepository }
├── shared/                database, errors, auth
└── main.ts
```
Create: `npx @nestjs/cli new acme`, then `nest g module orders`, `nest g controller orders`. `@nestjs/cqrs` is optional for CQRS.
Express: same folders under `src/features/orders/` with a `router.ts` per feature, zod for validation, and a small composition root (`src/app.ts`) that wires implementations into handlers by constructor/factory injection.
Guardrail: `dependency-cruiser` rule forbidding `domain/` from importing `infrastructure/`, `api/`, or framework packages; run it in CI.

## Python - FastAPI

```
src/acme/
├── domain/            orders.py (dataclasses / plain classes, no FastAPI or SQLAlchemy imports)
├── application/       create_order.py (use case), ports.py (typing.Protocol repositories)
├── infrastructure/    db.py, sqlalchemy_order_repository.py
└── api/               orders.py (APIRouter, Pydantic request/response models), main.py
tests/
```
Vertical Slice alternative: `src/acme/features/orders/{router.py, schemas.py, service.py, models.py}`.
Wire implementations through FastAPI `Depends(...)` providers in `api/` (the composition root); the use case receives a `Protocol`, not a concrete repository. Use `uv` or Poetry with a `pyproject.toml`.
Guardrail: `import-linter` with a `layers` contract (`acme.api` > `acme.infrastructure` > `acme.application` > `acme.domain`), run in CI via `lint-imports`.

## Java / Kotlin - Spring Boot

Package-by-feature with layers inside (single module), or one Gradle/Maven module per layer for hard boundaries:
```
com.acme.orders
├── domain/            Order.java, OrderLine.java (no Spring/JPA annotations)
├── application/       CreateOrderUseCase.java, OrderRepository.java (port interface)
├── infrastructure/    JpaOrderRepository.java, OrderEntity.java, OrderJpaMapper.java
└── api/               OrderController.java, CreateOrderRequest.java (record + Bean Validation)
```
Start from Spring Initializr (start.spring.io) with Web, Validation, Data JPA. Use constructor injection; the use case depends on the `OrderRepository` interface. For modular monoliths, Spring Modulith can verify module boundaries.
Guardrail: ArchUnit, e.g. `layeredArchitecture().consideringAllDependencies().layer("Domain").definedBy("..domain..")...whereLayer("Domain").mayNotAccessAnyLayer()` in a JUnit test.

## Go

```
cmd/api/main.go                 # composition root: config, DB, wiring, HTTP server
internal/
├── orders/
│   ├── order.go                # domain type + rules (no net/http, no SQL)
│   ├── service.go              # use cases; depends on the Repository interface it declares
│   ├── postgres.go             # Repository implementation (database/sql, pgx, sqlc, GORM)
│   └── http.go                 # handlers (net/http, chi, Gin, or Echo)
└── platform/                   # db, logging, middleware
go.mod
```
Go favors package-per-feature (a vertical slice) with small interfaces declared by the consumer; avoid deep `domain/application/infrastructure` trees unless the team wants them. `internal/` prevents imports from outside the module. Pass `context.Context` through every layer.
Guardrail: a test that runs `go list -deps` on the domain/service package and fails if it imports `net/http` or database packages, or a linter such as `depguard` (golangci-lint) rules.

## Output format

```
## Structure
<chosen style + why, tree>

## Commands
<shell commands to create and wire it>

## Key files
<sample feature end to end + DI wiring>

## Guardrail
<architecture test + how to run it>
```

## Principles

- Match the style to the team and problem; push back on over-engineering.
- One *working* sample feature beats a diagram - it's the template for everything else.
- Enforce the dependency rule with a test, not a wiki page.
- Keep the domain free of framework, ORM, and transport dependencies.
- In an existing codebase, consistency with the house style beats your preferred pattern.
