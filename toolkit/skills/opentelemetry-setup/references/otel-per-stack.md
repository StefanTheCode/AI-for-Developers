# OpenTelemetry wiring - Node.js, Python, Java/Kotlin, Go

Full wiring for non-.NET stacks (the .NET wiring is in `SKILL.md`). All examples read the endpoint, protocol, service name and sampler from the standard `OTEL_*` env vars, so the same code runs locally and in production. Pin package versions with the project's normal tooling; don't copy versions from blog posts.

## Node.js / TypeScript

```
npm i @opentelemetry/api @opentelemetry/sdk-node @opentelemetry/auto-instrumentations-node \
      @opentelemetry/exporter-trace-otlp-http @opentelemetry/exporter-metrics-otlp-http @opentelemetry/sdk-metrics
```

```ts
// instrumentation.ts - must run before the app imports http/express/pg
import { NodeSDK } from '@opentelemetry/sdk-node';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-http';
import { PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';

const sdk = new NodeSDK({
  traceExporter: new OTLPTraceExporter(),
  metricReader: new PeriodicExportingMetricReader({ exporter: new OTLPMetricExporter() }),
  instrumentations: [getNodeAutoInstrumentations({
    '@opentelemetry/instrumentation-fs': { enabled: false }, // very noisy
  })],
});
sdk.start();
process.on('SIGTERM', () => { sdk.shutdown().finally(() => process.exit(0)); });
```

Load it first: `node --require ./dist/instrumentation.js dist/server.js` (CommonJS) or `node --import ./dist/instrumentation.js dist/server.js` (ESM). Zero-code alternative: `node --require @opentelemetry/auto-instrumentations-node/register app.js` with `OTEL_*` env vars. The HTTP exporters default to port 4318.

```ts
import { trace, metrics, SpanStatusCode } from '@opentelemetry/api';
const tracer = trace.getTracer('orders-api');
const ordersPlaced = metrics.getMeter('orders-api').createCounter('orders.placed', { unit: '{orders}' });

export async function placeOrder(order: Order) {
  return tracer.startActiveSpan('PlaceOrder', async (span) => {
    span.setAttribute('order.id', order.id);
    try {
      await repo.save(order);
      ordersPlaced.add(1, { region: order.region });
    } catch (err) {
      span.recordException(err as Error);
      span.setStatus({ code: SpanStatusCode.ERROR });
      throw err;
    } finally {
      span.end();
    }
  });
}
```
Logs: pino and winston have OTel instrumentations (included in auto-instrumentations) that inject `trace_id`/`span_id` into log records.

## Python (FastAPI, Django, Flask)

```
pip install opentelemetry-distro opentelemetry-exporter-otlp
opentelemetry-bootstrap -a install     # installs instrumentations for the libraries it finds
```

Zero-code run:
```
OTEL_SERVICE_NAME=orders-api \
OTEL_TRACES_EXPORTER=otlp OTEL_METRICS_EXPORTER=otlp OTEL_LOGS_EXPORTER=otlp \
OTEL_PYTHON_LOGGING_AUTO_INSTRUMENTATION_ENABLED=true \
opentelemetry-instrument uvicorn app.main:app --host 0.0.0.0 --port 8000
```
Django: `opentelemetry-instrument python manage.py runserver --noreload`. Gunicorn/uWSGI pre-fork: configure the SDK in the post-fork hook (per worker) instead of the master process.

Manual (when you need code control, e.g. custom sampler):
```python
from opentelemetry import trace, metrics
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter

provider = TracerProvider(resource=Resource.create({"service.name": "orders-api"}))
provider.add_span_processor(BatchSpanProcessor(OTLPSpanExporter()))
trace.set_tracer_provider(provider)
```

Custom span and metric:
```python
from opentelemetry import trace, metrics
from opentelemetry.trace import Status, StatusCode

tracer = trace.get_tracer(__name__)
orders_placed = metrics.get_meter(__name__).create_counter("orders.placed", unit="{orders}")

async def place_order(order):
    with tracer.start_as_current_span("PlaceOrder") as span:
        span.set_attribute("order.id", order.id)
        try:
            await repo.save(order)
            orders_placed.add(1, {"region": order.region})
        except Exception as ex:
            span.record_exception(ex)
            span.set_status(Status(StatusCode.ERROR))
            raise
```

## Java / Kotlin (Spring Boot and others)

**Java agent (default, zero-code):**
```
java -javaagent:/otel/opentelemetry-javaagent.jar -jar app.jar
# env: OTEL_SERVICE_NAME, OTEL_EXPORTER_OTLP_ENDPOINT, OTEL_RESOURCE_ATTRIBUTES, OTEL_TRACES_SAMPLER(_ARG)
```
In Docker, download the agent jar from the opentelemetry-java-instrumentation GitHub releases at build time (pin the release), and set `JAVA_TOOL_OPTIONS=-javaagent:/otel/opentelemetry-javaagent.jar`. Disable a noisy instrumentation with `OTEL_INSTRUMENTATION_<NAME>_ENABLED=false`.

**Spring Boot starter (no agent, works with GraalVM native image):** add `io.opentelemetry.instrumentation:opentelemetry-spring-boot-starter` (use the OpenTelemetry instrumentation BOM for versions) and configure via `otel.*` properties or `OTEL_*` env vars.

**Already on Micrometer (Spring Boot 3):** Micrometer Tracing with `micrometer-tracing-bridge-otel` + an OTLP exporter is a valid alternative; don't mix it with the agent in the same app.

Custom span and metric:
```java
import io.opentelemetry.api.GlobalOpenTelemetry;
import io.opentelemetry.api.common.AttributeKey;
import io.opentelemetry.api.common.Attributes;
import io.opentelemetry.api.metrics.LongCounter;
import io.opentelemetry.api.trace.*;
import io.opentelemetry.context.Scope;
import io.opentelemetry.instrumentation.annotations.WithSpan;
import io.opentelemetry.instrumentation.annotations.SpanAttribute;

@WithSpan("PlaceOrder")                       // simplest: annotation (agent or starter)
public void placeOrder(@SpanAttribute("order.id") String orderId) { ... }

// explicit
Tracer tracer = GlobalOpenTelemetry.getTracer("orders-api");
LongCounter ordersPlaced = GlobalOpenTelemetry.getMeter("orders-api")
    .counterBuilder("orders.placed").setUnit("{orders}").build();

Span span = tracer.spanBuilder("PlaceOrder").startSpan();
try (Scope ignored = span.makeCurrent()) {
    span.setAttribute("order.id", order.getId());
    repo.save(order);
    ordersPlaced.add(1, Attributes.of(AttributeKey.stringKey("region"), order.getRegion()));
} catch (Exception e) {
    span.recordException(e);
    span.setStatus(StatusCode.ERROR);
    throw e;
} finally {
    span.end();
}
```
Logs: the agent bridges Logback/Log4j2 and adds `trace_id`/`span_id` to the MDC.

## Go

```
go get go.opentelemetry.io/otel go.opentelemetry.io/otel/sdk \
  go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracegrpc \
  go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp
```

```go
func setupOTel(ctx context.Context) (func(context.Context) error, error) {
	exp, err := otlptracegrpc.New(ctx) // reads OTEL_EXPORTER_OTLP_* env vars
	if err != nil {
		return nil, err
	}
	res, err := resource.New(ctx,
		resource.WithFromEnv(), // OTEL_SERVICE_NAME, OTEL_RESOURCE_ATTRIBUTES
		resource.WithAttributes(attribute.String("service.name", "orders-api")),
	)
	if err != nil {
		return nil, err
	}
	tp := sdktrace.NewTracerProvider(
		sdktrace.WithBatcher(exp),
		sdktrace.WithResource(res),
		sdktrace.WithSampler(sdktrace.ParentBased(sdktrace.TraceIDRatioBased(0.1))),
	)
	otel.SetTracerProvider(tp)
	otel.SetTextMapPropagator(propagation.NewCompositeTextMapPropagator(
		propagation.TraceContext{}, propagation.Baggage{}))
	return tp.Shutdown, nil
}

// main: shutdown, _ := setupOTel(ctx); defer shutdown(context.Background())
// server: http.Handle("/orders", otelhttp.NewHandler(ordersHandler, "orders"))
// client: client := &http.Client{Transport: otelhttp.NewTransport(http.DefaultTransport)}
```
Metrics follow the same shape with `sdkmetric.NewMeterProvider` + `sdkmetric.NewPeriodicReader` + the `otlpmetricgrpc` exporter, then `otel.SetMeterProvider`.

Custom span and metric:
```go
var tracer = otel.Tracer("orders-api")

func (s *Service) PlaceOrder(ctx context.Context, o Order) error {
	ctx, span := tracer.Start(ctx, "PlaceOrder")
	defer span.End()
	span.SetAttributes(attribute.String("order.id", o.ID))
	if err := s.repo.Save(ctx, o); err != nil { // pass ctx down, always
		span.RecordError(err)
		span.SetStatus(codes.Error, err.Error())
		return err
	}
	s.ordersPlaced.Add(ctx, 1, metric.WithAttributes(attribute.String("region", o.Region)))
	return nil
}
// ordersPlaced, _ := otel.Meter("orders-api").Int64Counter("orders.placed")
```
Logs: `log/slog` with the OTel slog bridge (`otelslog`), or add `trace_id` from `trace.SpanContextFromContext(ctx)` to your zap/zerolog fields.
