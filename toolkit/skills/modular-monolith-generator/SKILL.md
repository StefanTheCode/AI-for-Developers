---
name: modular-monolith-generator
description: Structure an app in any stack as a modular monolith - independent modules with enforced boundaries, small public contracts, internal-by-default code, and in-process messaging between modules - for .NET (internal + .Public projects), Java/Kotlin (Spring Modulith), TypeScript (NestJS modules, Nx, folder modules with index.ts), Python (packages + import-linter), and Go (internal/ packages). Use this skill whenever the user wants a modular monolith, module boundaries, to split a monolith or "big ball of mud" into modules, internal-by-default modules, module contracts / public APIs, Spring Modulith, or asks "how do I structure a modular monolith / keep modules decoupled". Always use this skill for modular-monolith requests; it sets up modules, boundaries, and a guardrail test.
category: Architecture
version: 1.0.0
---

# Modular Monolith Generator

Set up (or tighten) a modular monolith: each module owns its domain and data, exposes a small public contract, keeps everything else internal, and talks to other modules through that contract or in-process events - module independence without microservice overhead.

## Input - point it at your project
Works on a target: a **file**, **folder**, **project/repo**, or **GitHub URL**. If modules exist, audit their boundaries (who imports whose internals, who reads whose tables); if not, propose a split by business capability, not by technical layer.

## Step 1 - detect the stack

| Manifest | Stack | Module unit | Boundary mechanism | Guardrail |
|---|---|---|---|---|
| `*.csproj` / `*.sln` | C# / .NET | project per module (+ `.Public` contracts project) | `internal` by default, project references | NetArchTest / ArchUnitNET |
| `pom.xml` / `build.gradle(.kts)` | Java / Kotlin | top-level package per module (Spring Modulith) or Gradle/Maven module | package-private, `internal` subpackages | Spring Modulith `verify()`, ArchUnit |
| `package.json` | TypeScript / Node | folder or workspace package per module; NestJS `@Module` | `index.ts` public API, NestJS `exports`, package `exports` field | dependency-cruiser, eslint-plugin-boundaries, `@nx/enforce-module-boundaries` |
| `pyproject.toml` | Python | top-level package per module | `__init__.py` re-exports the public API, `_private` naming | import-linter (`independence`, `forbidden`) |
| `go.mod` | Go | directory per module | `internal/` under each module (compiler-enforced) | depguard / go-arch-lint |

Map the current structure with the manifest files, directory tree, and an import scan before proposing anything.

## Universal rules (every stack)
1. **Modules follow business capabilities** (Orders, Billing, Catalog), each with its own domain, use cases, and persistence.
2. **Internal by default.** Only the contract is public: a facade/service interface, DTOs, and integration events.
3. **Cross-module calls go through the contract** (synchronous facade) or **integration events** on an in-process bus (asynchronous). Never import another module's internals.
4. **Each module owns its data.** Separate schema or clearly owned tables; no cross-module joins or foreign keys; no reading another module's tables.
5. **The host/composition root wires modules** and is the only place that knows all of them.
6. **Tiny shared kernel** (Result type, base IDs, clock). It's a coupling magnet - keep it small.
7. **Boundaries are enforced by an automated check in CI**, not by documentation.
8. **It's a migration path.** Clean boundaries make a later extraction to a service cheap. Don't pay the distributed-systems tax until you need it.

## C# / .NET
```
src/
├── Modules/
│   ├── Orders/
│   │   ├── Orders.Public/        # contracts other modules may use (DTOs, integration events, a facade interface)
│   │   └── Orders/               # internal: domain, handlers, EF - internal by default
│   ├── Billing/
│   │   ├── Billing.Public/
│   │   └── Billing/
├── Shared/                       # truly shared kernel only (Result, base types)
└── Host/                         # composition root: wires every module
```
```csharp
public interface IModule
{
    IServiceCollection RegisterModule(IServiceCollection services, IConfiguration config);
    void MapEndpoints(IEndpointRouteBuilder app);
}
// Host discovers and calls every IModule - no module references another module's internals.
```
- A module references only other modules' **`.Public`** projects. Its own types are `internal`; use `InternalsVisibleTo` only for its test project.
- In-process events: MediatR notifications (check its license for new adoption), a custom in-memory bus, or `System.Threading.Channels`; for reliability, an outbox per module.
- One `DbContext` and schema per module (`modelBuilder.HasDefaultSchema("orders")`).

Guardrail (NetArchTest):
```csharp
[Fact]
public void Billing_does_not_reference_Orders_internals()
{
    var result = Types.InAssembly(typeof(BillingModule).Assembly)
        .Should().NotHaveDependencyOn("Company.Modules.Orders") // internal assembly, not Orders.Public
        .GetResult();
    Assert.True(result.IsSuccessful);
}
```

## Java / Kotlin - Spring Modulith
```
com.acme.shop
├── ShopApplication.java
├── orders/                 # module: public types here are the API
│   ├── OrderService.java   # public facade
│   ├── OrderPlaced.java    # public integration event
│   └── internal/           # implementation, not accessible to other modules
└── billing/
    └── internal/
```
```java
@Test
void verifiesModularStructure() {
    ApplicationModules.of(ShopApplication.class).verify();
}
```
Events: publish with `ApplicationEventPublisher`, consume with `@ApplicationModuleListener` (async, after commit, tracked by the event publication registry so failed listeners can be retried). Prefer package-private classes. Without Spring Modulith, use ArchUnit `slices().matching("com.acme.shop.(*)..").should().notDependOnEachOther()` plus explicit allowed-dependency rules.

## TypeScript / Node
```
src/modules/
├── orders/
│   ├── index.ts            # public API: facade, DTO types, event types - the only importable file
│   ├── orders.module.ts    # NestJS: exports only the facade provider
│   ├── domain/  application/  infrastructure/
└── billing/
```
NestJS: a module `exports` only its facade; other modules import the module, not its providers' files. In-process events via `@nestjs/event-emitter` or the `EventBus` from `@nestjs/cqrs`; plain Node via a typed `EventEmitter` wrapper. Enforce "import only `modules/<x>/index.ts`" with dependency-cruiser or eslint-plugin-boundaries; in an Nx or pnpm workspace, make each module a package with an `exports` field and tag-based `@nx/enforce-module-boundaries`. One schema or table prefix per module (Prisma multi-schema or separate clients if the project already does that).

## Python
```
shop/
├── orders/
│   ├── __init__.py         # re-exports the public API only (facade functions, DTOs, events)
│   ├── _domain.py  _service.py  _repository.py
└── billing/
```
In-process events: a simple publish/subscribe registry, or Django signals in Django projects (keep handlers in the subscribing module). Django: one app per module, no cross-app model imports or foreign keys except through the public API (store foreign IDs as plain values). Guardrail with import-linter:
```toml
# Fully independent modules (they talk only via events/the host):
[[tool.importlinter.contracts]]
name = "Modules are independent"
type = "independence"
modules = ["shop.orders", "shop.billing", "shop.catalog"]

# Or, when billing may call the orders public API but not its internals:
[[tool.importlinter.contracts]]
name = "Billing uses only the Orders public API"
type = "forbidden"
source_modules = ["shop.billing"]
forbidden_modules = ["shop.orders._domain", "shop.orders._service", "shop.orders._repository"]
```

## Go
```
internal/
├── orders/
│   ├── orders.go           # public API: Service interface, DTOs, events
│   └── internal/           # domain, storage - importable only from internal/orders/...
├── billing/
│   ├── billing.go
│   └── internal/
└── platform/               # shared kernel: config, db, eventbus
cmd/shop/main.go            # composition root
```
The compiler enforces `orders/internal` for free. For sibling rules ("billing must not import orders except its root package"), add depguard or go-arch-lint. In-process events: a small typed pub/sub in `platform/eventbus`, or Watermill with its in-memory Go channel pub/sub if the project may later move to a broker.

## Output format
```
## Detected
<stack, current modules (or proposed split), boundary violations found>

## Structure
<tree + what is public vs internal per module>

## Files
<module registration/wiring, contracts, event bus, data ownership changes>

## Guardrail
<the test/config that fails CI when a module reaches into another, and how to run it>

## Leaks to fix
<existing cross-module imports, shared tables, cross-module joins - with the fix for each>
```

## Principles
- **Internal by default.** If it's not in the contract, no other module can touch it.
- **Boundaries enforced by tests or linters**, not documentation - otherwise they erode.
- **Data ownership is the hardest boundary** - shared tables are the most common leak; flag them first.
- Keep the shared kernel tiny.

## How to use it & best prompts
"Structure this app as a modular monolith", "audit my module boundaries", "split this monolith into Orders/Billing/Catalog modules with contracts", "set up Spring Modulith for this app", "NestJS modules that only expose a facade", "import-linter contracts so my Django apps stay independent", "Go module layout with internal/ packages", "add a guardrail test so modules can't reach into each other". Pairs with `architecture-test-generator` (more rules) and the `architecture-reviewer` agent.
