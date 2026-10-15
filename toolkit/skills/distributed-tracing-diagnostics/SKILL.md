---
name: distributed-tracing-diagnostics
description: Diagnose and fix broken distributed traces in any stack (.NET, Node.js, Python, Java/Kotlin, Go) - missing spans, broken context propagation across HTTP/gRPC/messaging, orphaned or disconnected traces, unregistered tracers, sampling gaps, and context lost across threads/async boundaries. Use this skill whenever the user has traces that don't connect across services, missing spans, broken trace context, traceparent not propagating, propagation issues over Kafka/RabbitMQ/SQS/NATS, ActivitySource spans not showing up, context lost in goroutines/executors/Task.Run, or asks "why is my trace broken / spans not linking / context not propagating". Always use this skill for tracing diagnostics; it finds where the trace chain breaks.
category: Observability
version: 1.0.0
---

# Distributed Tracing Diagnostics

Find where a distributed trace breaks - the missing span, the dropped context, the uninstrumented hop - and fix propagation so one request shows as one connected trace across services, whatever language each service is written in.

## Input - point it at your project(s)
Works on a target: a **project**, multiple services, or a **GitHub URL**. Breaks happen at boundaries, so ask for (or scan) every service on the broken path, both the caller and the callee.

## Step 1 - detect each service's stack and tracing setup

| Manifest | Stack | Grep for |
|---|---|---|
| `*.csproj` | .NET | `ActivitySource\|AddSource\|StartActivity\|Propagator\|AddOpenTelemetry\|WithTracing` |
| `package.json` | Node / TS | `@opentelemetry/\|startActiveSpan\|propagation\.\(inject\|extract\)\|NodeSDK` |
| `pyproject.toml`, `requirements*.txt` | Python | `opentelemetry\|start_as_current_span\|propagate import\|opentelemetry-instrument` |
| `pom.xml`, `build.gradle(.kts)` | Java / Kotlin | `javaagent\|opentelemetry\|@WithSpan\|getTextMapPropagator\|micrometer-tracing` |
| `go.mod` | Go | `otel.Tracer\|SetTextMapPropagator\|otelhttp\|otelgrpc\|context.Background()` |

## Where traces break (checklist - run it per service and per hop)

1. **No tracing registered** - metrics-only setup, SDK never started, or the agent/instrument wrapper not actually on the start command. Fix with `opentelemetry-setup`.
2. **Custom tracer/source not registered or SDK loaded too late** - .NET: `StartActivity` returns null until `AddSource("name")`. Node: SDK started after `express`/`http` were imported, so they were never patched. Python: app started without `opentelemetry-instrument` or SDK configured after the framework app object is created.
3. **No propagator configured** - Go's default global propagator is a no-op: without `otel.SetTextMapPropagator(propagation.TraceContext{}...)` nothing is injected or extracted. Mixed systems: one side speaks W3C `traceparent`, the other only B3 or a vendor header - configure a composite propagator.
4. **HTTP context not propagated** - an uninstrumented client drops `traceparent`. .NET's HttpClient instrumentation covers every `HttpClient`; the usual culprits are a plain `http.Client` without `otelhttp.NewTransport` in Go, an HTTP library the Node/Python auto-instrumentation doesn't cover, a non-standard header, or a gateway/proxy stripping headers. Check that the outbound request actually carries the header.
5. **Messaging hops lose context** - queues and buses (Kafka, RabbitMQ, SQS, NATS, Service Bus) often don't auto-propagate, especially with custom producer/consumer wrappers. **Inject** into message headers on publish, **extract** on consume and start a consumer span with the extracted parent (code below).
6. **Context lost across async/thread boundaries** - .NET: `Activity.Current` not flowing into a custom thread or a fire-and-forget `Task.Run` started outside the request. Java: work submitted to a non-instrumented executor or a reactive chain without context propagation. Python: `ThreadPoolExecutor`/threads don't carry `contextvars` unless you copy the context. Node: callback-based libraries escaping the async context. Go: passing `context.Background()` (or no `ctx`) instead of the request `ctx`.
7. **Sampling drops spans** - inconsistent samplers between services (one is parent-based, one samples independently), or a low ratio at the root. Use parent-based sampling everywhere; decide at the edge or with tail sampling at the collector.
8. **Spans exported but never flushed** - short-lived jobs, CLIs, and serverless functions exit before the batch exporter flushes. Call the provider's shutdown/force-flush on exit.
9. **Wrong resource / service.name** - spans arrive but under `unknown_service` or a duplicated name, so the trace looks broken in the UI.

## Inject / extract for messaging, per stack

```csharp
// .NET - publish
var props = new Dictionary<string,string>();
Propagators.DefaultTextMapPropagator.Inject(
    new PropagationContext(activity.Context, Baggage.Current), props, (c,k,v) => c[k]=v);
// .NET - consume
var parent = Propagators.DefaultTextMapPropagator.Extract(default, props,
    (c,k) => c.TryGetValue(k, out var v) ? new[] { v } : Enumerable.Empty<string>());
using var act = source.StartActivity("orders process", ActivityKind.Consumer, parent.ActivityContext);
```
```ts
// Node - publish / consume
import { context, propagation, trace, SpanKind } from '@opentelemetry/api';
const headers: Record<string, string> = {};
propagation.inject(context.active(), headers);
// consumer:
const parentCtx = propagation.extract(context.active(), msg.headers);
tracer.startActiveSpan('orders process', { kind: SpanKind.CONSUMER }, parentCtx, (span) => { /* ... */ span.end(); });
```
```python
# Python - publish / consume
from opentelemetry.propagate import inject, extract
from opentelemetry.trace import SpanKind
headers = {}
inject(headers)
# consumer:
ctx = extract(message.headers)
with tracer.start_as_current_span("orders process", context=ctx, kind=SpanKind.CONSUMER):
    ...
```
```java
// Java - publish / consume (the Java agent already does this for common Kafka/JMS/RabbitMQ clients)
TextMapPropagator p = GlobalOpenTelemetry.getPropagators().getTextMapPropagator();
p.inject(Context.current(), headers, (carrier, key, value) -> carrier.put(key, value));
// consumer:
Context parent = p.extract(Context.current(), headers, getter);   // TextMapGetter over your header map
Span span = tracer.spanBuilder("orders process").setParent(parent).setSpanKind(SpanKind.CONSUMER).startSpan();
```
```go
// Go - publish / consume
otel.GetTextMapPropagator().Inject(ctx, propagation.MapCarrier(headers))
// consumer:
ctx := otel.GetTextMapPropagator().Extract(context.Background(), propagation.MapCarrier(headers))
ctx, span := tracer.Start(ctx, "orders process", trace.WithSpanKind(trace.SpanKindConsumer))
defer span.End()
```
For batch consumers, prefer span **links** to each message's context over picking one arbitrary parent.

## Async boundary fixes, per stack

| Stack | Symptom | Fix |
|---|---|---|
| .NET | spans from background work are orphaned | start work while `Activity.Current` is set, or pass `ActivityContext` and use it as the explicit parent |
| Node | context lost in callback-style libs | use promise APIs, or `context.with(ctx, fn)` / `context.bind(ctx, fn)` |
| Python | thread pool work has no parent | `contextvars.copy_context().run(fn)` per task, or the OTel threading instrumentation |
| Java | executor/reactive work has no parent | instrumented executors (agent), `Context.current().wrap(runnable)`, Reactor context propagation |
| Go | new root traces mid-request | pass the request `ctx` everywhere; never `context.Background()` inside a request |

## Principles
- A broken trace is almost always a **propagation** problem at a boundary (HTTP, queue, thread, language).
- Register every custom tracer/source and start the SDK before the code it instruments.
- Messaging needs **inject/extract** unless an auto-instrumentation demonstrably does it - verify, don't assume.
- One propagation format across the system (W3C TraceContext), parent-based sampling everywhere.
- Verify in the backend (Jaeger/Tempo/vendor) that parent span ids line up after the fix.

## Output format

```
## Trace path
<service A (stack) -> hop -> service B (stack) -> ...>

## Breaks found
1. [Propagation | Registration | Messaging | Async | Sampling | Flush] <service/file:line> - <what breaks> -> <fix>

## Fixes
<code per affected service>

## Verify
<what to look for in the tracing UI>
```

## How to use it & best prompts
"My traces don't connect across services", "spans from my code don't show up", "trace context is lost over Kafka/RabbitMQ/NATS", "my Go service starts a new trace for every call", "why is this trace orphaned". Pairs with `opentelemetry-setup` and `correlation-id-middleware`.
