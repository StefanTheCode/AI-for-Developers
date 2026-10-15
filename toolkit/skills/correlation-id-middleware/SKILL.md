---
name: correlation-id-middleware
description: Add correlation-ID (request-ID) middleware to an HTTP service in any stack - ASP.NET Core, Express/Fastify/NestJS, FastAPI/Django/Flask, Spring Boot, Go net/http (chi, gin, echo) - so every request has a traceable id flowing through logs, echoed to the caller, and propagated to downstream HTTP calls and messaging. Use this skill whenever the user wants a correlation id, request id, X-Correlation-ID / X-Request-ID header, to trace a request across services in logs, propagate an id to downstream calls or queues, put an id in the MDC/LogContext/contextvars, or asks "how do I add a correlation id / track a request through logs". Always use this skill for correlation-id requests; it wires middleware, log enrichment, and outbound propagation.
category: Observability
version: 1.0.0
---

# Correlation-ID Middleware

Give every request a correlation id that shows up in every log line, propagates to downstream HTTP/messaging, and ties a user's request together across services - even where full tracing isn't set up.

## Input - point it at your project
Works on a target: a **project**, a set of services, or a **GitHub URL**. Check for existing setup, the HTTP framework, the logger, and the outbound HTTP client before generating code.

## Step 1 - detect the stack

| Manifest | Stack | Grep for existing setup | Request-scoped storage | Log context |
|---|---|---|---|---|
| `*.csproj` | ASP.NET Core | `CorrelationId\|X-Correlation\|TraceIdentifier\|HttpClient` | `HttpContext.Items` | Serilog `LogContext` / `ILogger.BeginScope` |
| `package.json` | Express / Fastify / NestJS | `x-request-id\|x-correlation-id\|AsyncLocalStorage\|genReqId` | `AsyncLocalStorage` | pino child logger (`req.log`) |
| `pyproject.toml`, `requirements*.txt` | FastAPI / Starlette / Django / Flask | `correlation\|request_id\|contextvars` | `contextvars.ContextVar` | structlog contextvars / logging filter |
| `pom.xml`, `build.gradle(.kts)` | Spring Boot (MVC / WebFlux) | `MDC\|X-Correlation\|OncePerRequestFilter` | request attribute / Reactor context | SLF4J `MDC` |
| `go.mod` | net/http, chi, gin, echo | `X-Request-ID\|RequestID\|context.WithValue` | `context.Context` | `slog` attrs / zap fields |

## Universal design (apply in every stack)

1. **Accept inbound first.** If the caller or gateway sent `X-Correlation-ID` (or `X-Request-ID`), reuse it. Validate it (length cap, safe charset) - it's user input that lands in your logs.
2. **Otherwise reuse the W3C trace id** when tracing is active, so logs and traces share one id. Only generate a new UUID as the last resort.
3. **Store it request-scoped** (context / async-local storage / MDC) - never in a global or static field.
4. **Enrich logs once** at the middleware so every line carries it automatically.
5. **Echo it in the response header** so clients can quote it in bug reports.
6. **Propagate outbound** - every downstream HTTP call and every published message carries it; consumers restore it before handling.
7. **Register early** in the pipeline so auth failures and exception handlers log with the id too.

Pick one header name for the whole system and document it. Don't invent a parallel id that competes with real tracing - the correlation id is the log-grep companion to it (`distributed-tracing-diagnostics`).

## C# / .NET - ASP.NET Core

```csharp
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string Header = "X-Correlation-ID";
    public async Task Invoke(HttpContext ctx)
    {
        var id = ctx.Request.Headers.TryGetValue(Header, out var v) && !string.IsNullOrEmpty(v)
            ? v.ToString() : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

        ctx.Items[Header] = id;
        ctx.Response.Headers[Header] = id;                       // echo back to caller
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", id))  // enrich all logs
            await next(ctx);
    }
}
app.UseMiddleware<CorrelationIdMiddleware>();   // early in the pipeline
```
Without Serilog, use `using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))` with scopes enabled in the console/JSON formatter.

Outbound - a `DelegatingHandler` that copies the id onto `HttpClient` requests:
```csharp
public class CorrelationHandler(IHttpContextAccessor http) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        if (http.HttpContext?.Items[CorrelationIdMiddleware.Header] is string id)
            req.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.Header, id);
        return base.SendAsync(req, ct);
    }
}
// builder.Services.AddHttpContextAccessor();
// builder.Services.AddTransient<CorrelationHandler>();
// builder.Services.AddHttpClient<T>().AddHttpMessageHandler<CorrelationHandler>();
```

## TypeScript / Node.js - Express (Fastify/NestJS are analogous)

```ts
import { AsyncLocalStorage } from 'node:async_hooks';
import { randomUUID } from 'node:crypto';
import { trace } from '@opentelemetry/api';

export const HEADER = 'x-correlation-id';
export const als = new AsyncLocalStorage<{ correlationId: string }>();

export function correlationId(req, res, next) {
  const inbound = req.header(HEADER);
  const id = (inbound && /^[\w-]{1,128}$/.test(inbound) ? inbound : undefined)
    ?? trace.getActiveSpan()?.spanContext().traceId ?? randomUUID();
  res.setHeader(HEADER, id);
  req.log = req.log?.child({ correlationId: id });   // pino-http child logger
  als.run({ correlationId: id }, next);
}
// outbound: fetch(url, { headers: { [HEADER]: als.getStore()?.correlationId ?? '' } })
// axios: instance.interceptors.request.use(c => { c.headers[HEADER] = als.getStore()?.correlationId; return c; })
```
With pino-http, `genReqId` can do the accept-or-generate step and set `req.id`. Fastify has `genReqId` and `requestIdHeader` options built in.

## Python - FastAPI / Starlette

```python
import contextvars, re, uuid
from starlette.middleware.base import BaseHTTPMiddleware
import structlog

HEADER = "X-Correlation-ID"
correlation_id: contextvars.ContextVar[str] = contextvars.ContextVar("correlation_id", default="")

class CorrelationIdMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request, call_next):
        inbound = request.headers.get(HEADER, "")
        cid = inbound if re.fullmatch(r"[\w-]{1,128}", inbound) else uuid.uuid4().hex
        correlation_id.set(cid)
        structlog.contextvars.bind_contextvars(correlation_id=cid)
        response = await call_next(request)
        response.headers[HEADER] = cid
        return response

app.add_middleware(CorrelationIdMiddleware)
# outbound httpx (AsyncClient hooks must be async):
async def add_correlation_header(request):
    request.headers[HEADER] = correlation_id.get()
client = httpx.AsyncClient(event_hooks={"request": [add_correlation_header]})
```
The `asgi-correlation-id` package implements this for ASGI apps (plus a logging filter) - prefer it over hand-rolled code if the team is fine with the dependency. Django: a middleware class setting the same `ContextVar`; stdlib logging gets it via a `logging.Filter` that adds `record.correlation_id`.

## Java / Kotlin - Spring Boot

```java
@Component
@Order(Ordered.HIGHEST_PRECEDENCE)
public class CorrelationIdFilter extends OncePerRequestFilter {
    public static final String HEADER = "X-Correlation-ID";
    @Override
    protected void doFilterInternal(HttpServletRequest req, HttpServletResponse res, FilterChain chain)
            throws ServletException, IOException {
        String inbound = req.getHeader(HEADER);
        String id = (inbound != null && inbound.matches("[\\w-]{1,128}")) ? inbound : UUID.randomUUID().toString();
        MDC.put("correlationId", id);
        res.setHeader(HEADER, id);
        try { chain.doFilter(req, res); } finally { MDC.remove("correlationId"); }
    }
}
// outbound RestClient/RestTemplate: a ClientHttpRequestInterceptor that sets HEADER from MDC.get("correlationId")
```
- WebFlux: MDC is thread-bound and won't follow the reactive chain - use a `WebFilter` that writes to the Reactor `Context` and an `ExchangeFilterFunction` on `WebClient`; with Micrometer context propagation enabled, MDC can be restored automatically.
- `@Async` / executors: copy the MDC map into the task (a `TaskDecorator`).
- If Micrometer Tracing or the OTel agent is active, `traceId` is already in the MDC - consider using it as the correlation id.

## Go - net/http (chi, gin, echo are analogous)

```go
type ctxKey struct{}
const Header = "X-Correlation-ID"
var validID = regexp.MustCompile(`^[\w-]{1,128}$`)

func CorrelationID(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		id := r.Header.Get(Header)
		if !validID.MatchString(id) {
			if sc := trace.SpanContextFromContext(r.Context()); sc.HasTraceID() {
				id = sc.TraceID().String()
			} else {
				id = uuid.NewString() // github.com/google/uuid
			}
		}
		w.Header().Set(Header, id)
		ctx := context.WithValue(r.Context(), ctxKey{}, id)
		next.ServeHTTP(w, r.WithContext(ctx))
	})
}
func FromContext(ctx context.Context) string { s, _ := ctx.Value(ctxKey{}).(string); return s }
// logs: slog.InfoContext(ctx, ...) with a handler that adds FromContext(ctx), or logger.With("correlation_id", id)
// outbound: a RoundTripper that clones the request and sets Header from FromContext(req.Context())
```
chi ships `middleware.RequestID`; gin and echo have request-id middleware too - reuse them and just add log enrichment and outbound propagation.

## Messaging (all stacks)
Put the id in message headers/attributes on publish (Kafka headers, RabbitMQ/AMQP headers, SQS message attributes, NATS headers) and restore it into the request-scoped context and log context before the consumer handles the message. Same rule for background jobs enqueued from a request.

## Output format

```
## Current state
<stack, framework, logger, HTTP client, what exists>

## Middleware
<inbound accept-or-generate, store, log enrichment, echo>

## Outbound propagation
<HTTP client handler/interceptor + messaging publish/consume>

## Registration
<where to register it in the pipeline and why there>
```

## How to use it & best prompts
"Add a correlation id to my requests", "track a request through my logs", "propagate the correlation id to downstream services", "add X-Request-ID to my Express/FastAPI/Spring/Go service", "carry the id through Kafka messages". Pairs with `structured-logging-setup` and `distributed-tracing-diagnostics`.
