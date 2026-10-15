---
name: api-endpoint-scaffolder
description: Scaffold HTTP API endpoints in any stack - ASP.NET Core Minimal APIs or controllers, Express, Fastify, NestJS, FastAPI, Django REST Framework, Spring Boot controllers, Go net/http, chi, Gin - one feature per endpoint (REPR style) with request validation, typed responses, route grouping, auth, and OpenAPI metadata. Use this skill whenever the user wants to add an endpoint or route, convert controllers to minimal APIs, structure routes or routers, set up endpoint groups, TypedResults, route handlers, request DTOs/schemas (zod, Pydantic, Bean Validation, binding tags), or asks "how do I add an endpoint / structure my API routes". Always use this skill for endpoint scaffolding; it generates a complete, validated endpoint that matches the project's conventions.
category: Architecture
version: 1.0.0
---

# API Endpoint Scaffolder

Generate clean HTTP endpoints - one feature per endpoint (REPR: Request-Endpoint-Response), validated at the edge, with explicit response types, grouped routes, auth, and OpenAPI metadata - in the idiom of the project's framework.

## Input - point it at your project
Works on a target: a **file**, **folder**, **project/repo**, or **GitHub URL** (`git clone --depth 1 <url> /tmp/repo`). Always read one existing endpoint first and match its style (folder layout, naming, how it dispatches to business logic, how it validates and returns errors).

## Step 1 - detect the stack and framework

| Manifest | Look for | Framework | Find existing endpoints |
|---|---|---|---|
| `*.csproj` | `Microsoft.NET.Sdk.Web`, `Carter`, `FastEndpoints` | ASP.NET Core Minimal APIs / controllers | `grep -rn "MapGet\|MapPost\|MapGroup\|ICarterModule\|: ControllerBase" --include=*.cs` |
| `package.json` | `express`, `fastify`, `@nestjs/core` | Express / Fastify / NestJS | `grep -rn "router\.\(get\|post\)\|app\.\(get\|post\)\|@Controller(" --include=*.ts` |
| `pyproject.toml`, `requirements*.txt` | `fastapi`, `djangorestframework` | FastAPI / DRF | `grep -rn "APIRouter\|@router\.\|APIView\|ViewSet" --include=*.py` |
| `pom.xml`, `build.gradle(.kts)` | `spring-boot-starter-web` / `-webflux` | Spring Boot | `grep -rn "@RestController\|@RequestMapping" src` |
| `go.mod` | `go-chi/chi`, `gin-gonic/gin`, none | chi / Gin / net/http | `grep -rn "HandleFunc\|r\.Route\|\.POST(\|\.Group(" --include=*.go` |

## Universal principles (every stack)
1. **One endpoint per feature.** A handler file/class/function per use case (`CreateOrder`, `GetOrderById`), not a 2,000-line controller.
2. **Explicit request and response types.** A request DTO/schema and a response DTO - never return ORM entities directly.
3. **Validate at the edge.** Shape and format validation in the endpoint layer; business rules in the handler/domain. Return a consistent error body (RFC 9457 / 7807 Problem Details where the framework supports it).
4. **Thin endpoint.** Parse, validate, call one service/handler, map the result to HTTP. No data access or business logic inline.
5. **Correct status codes.** `201 Created` + `Location` for creates, `204` for no-content, `404` for missing, `409` for conflicts, `400/422` for validation.
6. **Group routes** for shared prefix, tags, auth, and middleware instead of repeating per endpoint.
7. **Document it.** OpenAPI metadata (name, summary, response types) generated from the code, not hand-written.
8. **Pass cancellation / context** through to I/O calls.

## C# / .NET - Minimal APIs (REPR + TypedResults)
```csharp
public static class CreateOrderEndpoint
{
    public static RouteGroupBuilder MapCreateOrder(this RouteGroupBuilder group)
    {
        group.MapPost("/", Handle)
             .WithName("CreateOrder")
             .WithSummary("Creates an order")
             .Produces<OrderResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem();
        return group;
    }

    public record Request(Guid CustomerId, List<OrderLine> Lines);
    public record OrderResponse(Guid Id);

    private static async Task<Results<Created<OrderResponse>, ValidationProblem>> Handle(
        Request req, IValidator<Request> validator, ISender sender, CancellationToken ct)
    {
        var v = await validator.ValidateAsync(req, ct);
        if (!v.IsValid) return TypedResults.ValidationProblem(v.ToDictionary());

        var id = await sender.Send(new CreateOrderCommand(req.CustomerId, req.Lines), ct);
        return TypedResults.Created($"/orders/{id}", new OrderResponse(id));
    }
}

// Program.cs - route grouping
var orders = app.MapGroup("/orders").WithTags("Orders").RequireAuthorization();
orders.MapCreateOrder();
orders.MapGetOrderById();
```
- **`TypedResults` over `Results`** - compile-time response types and accurate OpenAPI.
- Match the existing style: plain `MapGroup`, Carter modules, FastEndpoints, or controllers. If the project uses controllers and didn't ask to migrate, add a controller action instead.
- Don't hand-roll what `MapGroup` + endpoint filters already do.

## TypeScript / Node
**Express** (router + zod):
```ts
const CreateOrder = z.object({ customerId: z.string().uuid(), lines: z.array(Line).min(1) });

export const ordersRouter = Router().use(requireAuth);
ordersRouter.post("/", async (req, res, next) => {
  const parsed = CreateOrder.safeParse(req.body);
  if (!parsed.success) return res.status(400).json({ title: "Validation failed", errors: parsed.error.flatten() });
  try {
    const id = await orders.create(parsed.data);
    res.status(201).location(`/orders/${id}`).json({ id });
  } catch (e) { next(e); }
});
app.use("/orders", ordersRouter);
```
**Fastify** - declare `schema: { body, response: { 201: ... } }` on the route (JSON Schema, TypeBox, or a zod type provider) so validation and serialization come from the schema; group with `app.register(ordersRoutes, { prefix: "/orders" })`.
**NestJS** - `@Controller("orders")` + `@Post()` with a DTO validated by `class-validator` and a global `ValidationPipe({ whitelist: true })`; guards for auth; `@nestjs/swagger` decorators for OpenAPI. Delegate to a service (or `CommandBus` if `@nestjs/cqrs` is in use).

## Python
**FastAPI:**
```python
router = APIRouter(prefix="/orders", tags=["orders"], dependencies=[Depends(require_user)])

class CreateOrderRequest(BaseModel):
    customer_id: UUID
    lines: list[OrderLine] = Field(min_length=1)

class OrderResponse(BaseModel):
    id: UUID

@router.post("", response_model=OrderResponse, status_code=status.HTTP_201_CREATED)
async def create_order(req: CreateOrderRequest, response: Response,
                       svc: OrderService = Depends(get_order_service)) -> OrderResponse:
    order_id = await svc.create(req.customer_id, req.lines)
    response.headers["Location"] = f"/orders/{order_id}"
    return OrderResponse(id=order_id)
```
Pydantic validates the body (422 on failure). Include the router with `app.include_router(router)`.
**Django REST Framework** - a `Serializer` for input/output, a focused `APIView` or `ViewSet` action, `permission_classes` for auth, register via a `Router`; use drf-spectacular for OpenAPI if the project already does.

## Java / Kotlin - Spring Boot
```java
@RestController
@RequestMapping("/orders")
class CreateOrderController {
    private final OrderService orders;
    CreateOrderController(OrderService orders) { this.orders = orders; }

    @PostMapping
    ResponseEntity<OrderResponse> create(@Valid @RequestBody CreateOrderRequest req) {
        UUID id = orders.create(req.customerId(), req.lines());
        return ResponseEntity.created(URI.create("/orders/" + id)).body(new OrderResponse(id));
    }
}

record CreateOrderRequest(@NotNull UUID customerId, @NotEmpty List<@Valid OrderLine> lines) {}
record OrderResponse(UUID id) {}
```
Jakarta Bean Validation on the record; map errors to `ProblemDetail` in one `@RestControllerAdvice` (or enable Boot's built-in problem details). Auth via Spring Security rules on the path prefix; springdoc-openapi for docs. One controller per feature or a small cohesive controller - match the project.

## Go
**net/http (Go 1.22+ method patterns)**, **chi**, or **Gin** - match what `go.mod` has:
```go
type createOrderRequest struct {
    CustomerID string      `json:"customerId"`
    Lines      []orderLine `json:"lines"`
}

func (h *OrderHandler) Create(w http.ResponseWriter, r *http.Request) {
    var req createOrderRequest
    if err := json.NewDecoder(r.Body).Decode(&req); err != nil || len(req.Lines) == 0 {
        writeProblem(w, http.StatusBadRequest, "invalid request body")
        return
    }
    id, err := h.svc.Create(r.Context(), req.CustomerID, req.Lines)
    if err != nil { writeError(w, err); return }
    w.Header().Set("Location", "/orders/"+id)
    writeJSON(w, http.StatusCreated, map[string]string{"id": id})
}

// net/http:  mux.HandleFunc("POST /orders", h.Create)
// chi:       r.Route("/orders", func(r chi.Router) { r.Use(auth); r.Post("/", h.Create) })
```
**Gin** - `g := r.Group("/orders", auth); g.POST("", h.Create)`, request struct with `binding:"required"` tags and `c.ShouldBindJSON(&req)`. Use go-playground/validator if the project already does. Always pass `r.Context()` down.

## Output format
```
## Detected
<stack + framework + the existing endpoint style it matched>

## Files
<each new/changed file with full code: request/response types, handler, route registration>

## Wiring
<where it's registered (group/router/module) and any DI/config change>

## Notes
<validation rules assumed, auth applied, anything to verify>
```

## How to use it & best prompts
"Add an endpoint for creating X", "convert this controller to minimal APIs", "group these routes under /orders with auth", "add a FastAPI router for invoices", "scaffold a Spring controller for refunds", "add a chi route for GET /orders/{id}". Pairs with `cqrs-setup` (dispatch to handlers), `result-pattern-scaffolder` (map errors to HTTP in one place), and `authorization-setup`.
