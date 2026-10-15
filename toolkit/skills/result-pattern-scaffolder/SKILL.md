---
name: result-pattern-scaffolder
description: Add a Result/Error pattern (errors as values) to a codebase in any stack so expected failures stop using exceptions for control flow - Result<T> in C# (or ErrorOr / FluentResults / OneOf), discriminated unions or neverthrow in TypeScript, Ok/Err dataclasses or the returns library in Python, sealed interfaces / Vavr Either / Kotlin Result or Arrow in Java/Kotlin, and idiomatic typed/sentinel errors with errors.Is/As in Go - plus mapping domain errors to HTTP in one place. Use this skill whenever the user wants a Result type, Result<T>, Either, railway-oriented programming, OneOf, error handling without exceptions, typed errors, mapping domain errors to HTTP responses / Problem Details, or asks "how do I return errors cleanly / stop throwing for validation". Always use this skill for Result-pattern requests; it generates the type, helpers, and endpoint mapping.
category: Architecture
version: 1.0.0
---

# Result Pattern Scaffolder

Introduce errors-as-values so expected failures (validation, not-found, conflict, forbidden) are returned, not thrown - and map them to HTTP responses in exactly one place at the edge.

## Input - point it at your code
Works on a target: a **file**, **folder**, **project/repo**, or **GitHub URL** (`git clone --depth 1 <url>`). If a Result/Either type or error convention already exists, extend it instead of adding a second one.

## Step 1 - detect the stack and existing convention

| Manifest | Stack | Look for | Default |
|---|---|---|---|
| `*.csproj` | C# / .NET | `OneOf`, `ErrorOr`, `FluentResults`, `Result<`, `throw new .*Exception` | Small in-house `Result<T>` + `Error`, or the library already referenced |
| `package.json` | TypeScript | `neverthrow`, `Result<`, `{ ok: true`, `throw new` | Discriminated union `Result<T, E>` (or neverthrow if present) |
| `pyproject.toml` / `requirements*.txt` | Python | `returns`, `Ok(`/`Err(`, `raise .*Error` | Frozen `Ok`/`Err` dataclasses + `match`; or domain exceptions mapped centrally if that's the house style |
| `pom.xml` / `build.gradle(.kts)` | Java / Kotlin | `Either`, `sealed interface`, `kotlin.Result`, Arrow | Java: sealed interface + records; Kotlin: sealed class or Arrow `Either` |
| `go.mod` | Go | `errors.Is`, `errors.As`, `var Err` | **No Result type** - `(T, error)` with sentinel/typed domain errors |
| `Cargo.toml` | Rust | `thiserror`, `Result<` | Native `Result<T, E>` + a domain error enum |

Grep for the throw sites that represent expected outcomes (validation, not found, conflict) - those are the conversion candidates.

## Universal principles (every stack)
1. **Expected outcomes are values; bugs and infrastructure failures stay exceptions/panics.** "User typed a bad value" is a Result; "the DB is down" is an exception.
2. **A small, closed set of error kinds** (`validation`, `not_found`, `conflict`, `forbidden`, `unexpected`) with a stable code and message. Kinds drive HTTP mapping; codes are for clients.
3. **Map errors to HTTP in exactly one place** (an extension, middleware, exception/error handler, or `writeError` helper), returning Problem Details (RFC 9457 / 7807) consistently.
4. **Don't swallow real exceptions** into a generic Result - let them reach the global error handler and logging.
5. **Make the success path cheap to read.** Provide `map`/`bind` (or early returns) so callers don't nest `if` chains.
6. **One convention per codebase.** If `OneOf`, neverthrow, Arrow, or typed errors already exist, extend them.
7. **Callers must handle the failure.** Prefer types the compiler forces you to inspect (unions, sealed types, `(T, error)`).

## C# / .NET
```csharp
public readonly record struct Error(string Code, string Message, ErrorKind Kind)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorKind.None);
    public static Error NotFound(string msg)   => new("not_found", msg, ErrorKind.NotFound);
    public static Error Validation(string msg) => new("validation", msg, ErrorKind.Validation);
    public static Error Conflict(string msg)   => new("conflict", msg, ErrorKind.Conflict);
}

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error Error { get; }
    private Result(T v){ IsSuccess = true; Value = v; Error = Error.None; }
    private Result(Error e){ IsSuccess = false; Value = default; Error = e; }
    public static Result<T> Success(T v) => new(v);
    public static Result<T> Failure(Error e) => new(e);
    public static implicit operator Result<T>(T v) => Success(v);
    public static implicit operator Result<T>(Error e) => Failure(e);
}
```
**Handler:**
```csharp
public async Task<Result<OrderDto>> Handle(GetOrder q, CancellationToken ct)
{
    var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == q.Id, ct);
    if (order is null) return Error.NotFound($"Order {q.Id} not found");
    return _mapper.ToDto(order);
}
```
**Mapping to HTTP (one place):**
```csharp
public static IResult ToHttp<T>(this Result<T> r) => r.IsSuccess
    ? TypedResults.Ok(r.Value)
    : r.Error.Kind switch
    {
        ErrorKind.NotFound   => TypedResults.Problem(r.Error.Message, statusCode: 404),
        ErrorKind.Validation => TypedResults.Problem(r.Error.Message, statusCode: 400),
        ErrorKind.Conflict   => TypedResults.Problem(r.Error.Message, statusCode: 409),
        _                    => TypedResults.Problem(r.Error.Message)
    };
```
If the codebase already uses `OneOf<T, Error>`, ErrorOr, or FluentResults, extend that rather than introducing a competing type.

## TypeScript
```ts
export type Result<T, E = AppError> = { ok: true; value: T } | { ok: false; error: E };
export const ok = <T>(value: T): Result<T, never> => ({ ok: true, value });
export const err = <E>(error: E): Result<never, E> => ({ ok: false, error });

export type AppError =
  | { kind: "validation"; message: string; fields?: Record<string, string> }
  | { kind: "not_found"; message: string }
  | { kind: "conflict"; message: string };

const STATUS: Record<AppError["kind"], number> = { validation: 400, not_found: 404, conflict: 409 };

// one place: route helper / error-mapping middleware
export function send<T>(res: Response, r: Result<T>, okStatus = 200) {
  if (r.ok) return res.status(okStatus).json(r.value);
  return res.status(STATUS[r.error.kind])
    .type("application/problem+json")
    .json({ title: r.error.kind, detail: r.error.message, status: STATUS[r.error.kind] });
}
```
The union forces callers to check `r.ok`. If neverthrow is installed, use its `Result`/`ResultAsync` and `match` instead. NestJS: map in one exception filter or interceptor.

## Python
```python
@dataclass(frozen=True)
class Ok(Generic[T]):
    value: T

@dataclass(frozen=True)
class Err:
    kind: Literal["validation", "not_found", "conflict"]
    message: str

Result = Ok[T] | Err

async def get_order(order_id: UUID) -> Result[OrderDto]:
    order = await repo.get(order_id)
    if order is None:
        return Err("not_found", f"Order {order_id} not found")
    return Ok(OrderDto.from_domain(order))

STATUS = {"validation": 400, "not_found": 404, "conflict": 409}

def to_response(r: Result[Any], ok_status: int = 200) -> JSONResponse:   # one place
    match r:
        case Ok(value):
            return JSONResponse(jsonable_encoder(value), status_code=ok_status)
        case Err(kind, message):
            return JSONResponse({"title": kind, "detail": message, "status": STATUS[kind]},
                                status_code=STATUS[kind], media_type="application/problem+json")
```
Python culture leans on exceptions; if the codebase does too, the equivalent "one place" pattern is a small domain exception hierarchy plus a single `app.exception_handler(DomainError)` (FastAPI) or DRF `EXCEPTION_HANDLER`. Say which you chose and why. Use the `returns` library only if it's already in use.

## Java / Kotlin
```java
public sealed interface Result<T> permits Result.Ok, Result.Err {
    record Ok<T>(T value) implements Result<T> {}
    record Err<T>(AppError error) implements Result<T> {}
}
public record AppError(Kind kind, String message) { public enum Kind { VALIDATION, NOT_FOUND, CONFLICT } }

// one place: a small mapper used by controllers
static <T> ResponseEntity<?> toResponse(Result<T> r) {
    return switch (r) {
        case Result.Ok<T> ok -> ResponseEntity.ok(ok.value());
        case Result.Err<T> e -> {
            var status = switch (e.error().kind()) {
                case VALIDATION -> HttpStatus.BAD_REQUEST;
                case NOT_FOUND  -> HttpStatus.NOT_FOUND;
                case CONFLICT   -> HttpStatus.CONFLICT;
            };
            yield ResponseEntity.of(ProblemDetail.forStatusAndDetail(status, e.error().message())).build();
        }
    };
}
```
Needs Java 21 pattern matching for `switch`. If Vavr is in use, `Either<AppError, T>` is the equivalent. Kotlin: a `sealed class` result or Arrow's `Either` with `fold`; `kotlin.Result` only wraps `Throwable`, so it's a poor fit for domain errors. Teams that prefer exceptions: domain exceptions + one `@RestControllerAdvice` returning `ProblemDetail`.

## Go
Go already has errors as values - **don't introduce a generic Result type.** Make the errors typed and map them once:
```go
var (
    ErrNotFound = errors.New("not found")
    ErrConflict = errors.New("conflict")
)

type ValidationError struct{ Field, Msg string }
func (e *ValidationError) Error() string { return e.Field + ": " + e.Msg }

func (s *Service) GetOrder(ctx context.Context, id string) (OrderDTO, error) {
    o, err := s.repo.Get(ctx, id)
    if errors.Is(err, sql.ErrNoRows) { return OrderDTO{}, fmt.Errorf("order %s: %w", id, ErrNotFound) }
    if err != nil { return OrderDTO{}, err }   // infrastructure error, passes through
    return toDTO(o), nil
}

func writeError(w http.ResponseWriter, err error) {      // one place
    var ve *ValidationError
    switch {
    case errors.As(err, &ve):          writeProblem(w, http.StatusBadRequest, ve.Error())
    case errors.Is(err, ErrNotFound):  writeProblem(w, http.StatusNotFound, err.Error())
    case errors.Is(err, ErrConflict):  writeProblem(w, http.StatusConflict, err.Error())
    default:                           writeProblem(w, http.StatusInternalServerError, "internal error") // log err
    }
}
```

## Rust
Native `Result<T, E>` with a domain error enum (`thiserror`) and one `IntoResponse` impl (axum) or `ResponseError` impl (actix-web) for the HTTP mapping.

## Output format
```
## Detected
<stack, existing convention found, choice made and why>

## Files
<the Result/Error type (or error set), helpers, the single HTTP mapping>

## Conversions
| Location | Before (throw/raise/panic) | After |

## Left as exceptions
<what intentionally stays an exception and why>
```

## How to use it & best prompts
"Add a Result pattern to this project", "stop throwing for validation in these handlers", "map domain errors to HTTP in one place", "Result type for my TypeScript services", "errors as values in my FastAPI app", "sealed Result type for Java 21", "clean up error handling in my Go handlers with errors.Is". Pairs with `cqrs-setup` (handler return types), `api-endpoint-scaffolder` (edge mapping), and the `code-reviewer` agent.
