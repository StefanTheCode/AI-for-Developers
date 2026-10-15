---
name: cqrs-setup
description: Set up CQRS (command/query separation) in any stack - commands, queries, handlers, and a request pipeline for validation, logging, and transactions. Covers .NET (MediatR, a source-generated mediator, or a thin custom dispatcher), NestJS (@nestjs/cqrs CommandBus/QueryBus), and plain handlers in Express/Fastify, Python (FastAPI/Django), Spring Boot, and Go (optionally Watermill's CQRS component). Use this skill whenever the user wants CQRS, MediatR, a mediator, command/query handlers, a command bus or query bus, pipeline behaviors / middleware / decorators around handlers, or asks "how do I add CQRS / wire up MediatR / separate reads and writes". Always use this skill for CQRS requests; it wires a working pipeline rather than a snippet.
category: Architecture
version: 1.0.0
---

# CQRS Setup

Wire up CQRS: command/query separation, one handler per use case, and the cross-cutting pipeline (validation, logging, transactions) that makes CQRS pay off - using the idiom of the project's stack, with or without a mediator library.

## Input - point it at your project
Works on a target, not pasted snippets: a **file**, **folder**, **project/repo**, or **GitHub URL** (`git clone --depth 1 <url>` then scan). If the project exists, read one feature first and match its conventions (folder layout, naming, return types, error handling).

## Step 1 - detect the stack

| Manifest | Stack | Default approach |
|---|---|---|
| `*.csproj` | C# / .NET | MediatR if already referenced; otherwise offer MediatR (check license), a source-generated mediator, or a thin custom dispatcher |
| `package.json` with `@nestjs/core` | NestJS | `@nestjs/cqrs` (`CommandBus`, `QueryBus`, `@CommandHandler`) |
| `package.json` (Express/Fastify) | Node / TypeScript | Plain handler functions or classes + a small typed dispatcher or direct calls |
| `pyproject.toml` / `requirements*.txt` | Python | Plain handlers + a message-bus dict, decorators for cross-cutting concerns |
| `pom.xml` / `build.gradle(.kts)` | Java / Kotlin (Spring) | Plain `@Service` command handlers and query services; Spring AOP/`@Transactional`/Bean Validation for the pipeline; Axon Framework only for full CQRS/ES |
| `go.mod` | Go | Handler structs with `Handle(ctx, cmd)` + decorator wrappers; Watermill CQRS if commands go over a broker |

## Universal principles (every stack)
1. **Commands change state and return little** (an id, a Result); **queries read and never mutate.** Separate folders/packages.
2. **One command/query per file, one handler per command.** Name by intent: `CreateOrder`, `GetOrderById`.
3. **Cross-cutting concerns live in the pipeline** (behaviors, middleware, decorators, interceptors, AOP), not copy-pasted into every handler: validation, logging, transactions (commands only), authorization where relevant.
4. **Query handlers are read-optimized:** no change tracking, project straight to DTOs, can bypass the domain model and repositories.
5. **Transactions wrap commands only.** One command = one transaction = one aggregate change (plus outbox messages).
6. **Don't add a bus to a tiny CRUD app.** For a few endpoints, calling a handler/service directly is simpler. Say so.
7. **A mediator is optional.** CQRS is a separation of models and responsibilities; dispatch can be a direct call, a dictionary, DI, or a library.

## C# / .NET
**Choosing the dispatcher:** MediatR is the common default, but recent major versions moved to a commercial license (with a free tier for some organizations) - **check the license terms for the user's org before adding it**, or stay on the version already in use. Alternatives: a source-generated mediator (e.g. the `Mediator` package), Wolverine, or a thin custom dispatcher (an `ICommandHandler<TCommand, TResult>` interface resolved from DI plus decorators).

```
dotnet add package MediatR
dotnet add package FluentValidation.DependencyInjectionExtensions
```
```csharp
builder.Services.AddMediatR(c => c.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
```

**Command + handler:**
```csharp
public record CreateOrderCommand(Guid CustomerId, List<OrderLine> Lines) : IRequest<Result<Guid>>;

public class CreateOrderHandler(IOrderRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = Order.Create(cmd.CustomerId, cmd.Lines);
        await repo.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success(order.Id);
    }
}
```
Queries are the same shape with `IRequest<TDto>` and a read-only handler (`AsNoTracking`, projection).

**Pipeline behaviors (where CQRS earns its keep):**
```csharp
public class ValidationBehavior<TReq, TRes>(IEnumerable<IValidator<TReq>> validators)
    : IPipelineBehavior<TReq, TRes> where TReq : notnull
{
    public async Task<TRes> Handle(TReq req, RequestHandlerDelegate<TRes> next, CancellationToken ct)
    {
        foreach (var v in validators)
        {
            var r = await v.ValidateAsync(req, ct);
            if (!r.IsValid) throw new ValidationException(r.Errors); // or map to a Result
        }
        return await next();
    }
}
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```
Add `LoggingBehavior` and a `TransactionBehavior` (open a transaction around commands only) the same way. With a custom dispatcher, implement the same three as decorators (Scrutor's `Decorate` or manual registration).

## TypeScript / Node
**NestJS (`@nestjs/cqrs`):**
```ts
export class CreateOrderCommand { constructor(readonly customerId: string, readonly lines: OrderLine[]) {} }

@CommandHandler(CreateOrderCommand)
export class CreateOrderHandler implements ICommandHandler<CreateOrderCommand> {
  constructor(private readonly orders: OrderRepository) {}
  async execute(cmd: CreateOrderCommand): Promise<string> {
    const order = Order.create(cmd.customerId, cmd.lines);
    await this.orders.save(order);
    return order.id;
  }
}
// module: imports: [CqrsModule], providers: [CreateOrderHandler]
// controller: return this.commandBus.execute(new CreateOrderCommand(dto.customerId, dto.lines));
```
Queries use `QueryBus` + `@QueryHandler`. Validation stays in `ValidationPipe` at the controller; logging/transactions via interceptors or a wrapper around the handler.

**Express / Fastify (no framework bus):** plain handlers typed as `(cmd: C) => Promise<R>`, composed with higher-order wrappers - `withValidation(schema)`, `withTransaction(db)`, `withLogging(name)` - and called directly from the route. A map-based dispatcher is only worth it when something must dispatch by message type (queues, jobs).

## Python
Plain handlers plus a small bus, in the "Cosmic Python" message-bus style:
```python
@dataclass(frozen=True)
class CreateOrder:
    customer_id: UUID
    lines: list[OrderLine]

def create_order(cmd: CreateOrder, uow: UnitOfWork) -> UUID:
    with uow:
        order = Order.create(cmd.customer_id, cmd.lines)
        uow.orders.add(order)
        uow.commit()
    return order.id

COMMAND_HANDLERS = {CreateOrder: create_order}

def dispatch(cmd, uow):
    handler = COMMAND_HANDLERS[type(cmd)]
    log.info("handling", command=type(cmd).__name__)
    return handler(cmd, uow)
```
Validation comes from Pydantic models at the edge (FastAPI) or serializers (DRF); transactions from the unit of work or `transaction.atomic()` (Django); logging/metrics as decorators on handlers. Query handlers select columns straight into DTOs. Async: same shape with `async def`.

## Java / Kotlin - Spring
CQRS in Spring is usually just discipline, not a library: `OrderCommandService` methods (or one `@Service` per command) annotated `@Transactional`, and `OrderQueryService` with `@Transactional(readOnly = true)` returning DTO projections. Validation via `@Validated` + Bean Validation on command records; logging/metrics via an aspect or Micrometer `@Observed`. Separate read repositories (interface projections, JdbcClient/jOOQ) from the write-side JPA repositories. Reach for Axon Framework only when you also want event sourcing and distributed command/query buses.

## Go
```go
type CreateOrder struct { CustomerID string; Lines []OrderLine }

type CommandHandler[C any, R any] interface { Handle(ctx context.Context, cmd C) (R, error) }

type createOrderHandler struct { repo OrderRepository; tx TxManager }

func (h createOrderHandler) Handle(ctx context.Context, cmd CreateOrder) (string, error) {
    var id string
    err := h.tx.WithinTx(ctx, func(ctx context.Context) error {
        o, err := NewOrder(cmd.CustomerID, cmd.Lines)
        if err != nil { return err }
        id = o.ID
        return h.repo.Save(ctx, o)
    })
    return id, err
}
```
Cross-cutting concerns as decorators implementing the same interface (`loggingDecorator[C, R]`, `metricsDecorator`), composed in `main`/wiring. Keep `app/command` and `app/query` packages separate. Use Watermill's CQRS component only when commands/events travel over a message broker.

## Output format
```
## Detected
<stack, existing conventions, dispatcher chosen and why (incl. license note for MediatR)>

## Files
<commands/queries, handlers, pipeline pieces, registration - full code>

## Wiring
<DI/module registration and how endpoints call it>

## When not to
<anything that should stay a direct call>
```

## How to use it & best prompts
Point it at a project and say what you want: "add CQRS with MediatR + validation/transaction behaviors", "CQRS without MediatR", "convert these controllers to commands/queries", "set up @nestjs/cqrs for orders", "command handlers with a unit of work in my FastAPI app", "split my Spring service into command and query services", "command handlers with logging decorators in Go". For new feature slices pair with `clean-architecture-scaffolder`; for handler return types pair with `result-pattern-scaffolder`.
