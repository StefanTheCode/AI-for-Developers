# OTel Collector + Jaeger + Prometheus - local stack

Drop-in `docker-compose.yml` and collector config for local development. Works for any stack - .NET, Node, Python, Java, Go all export OTLP the same way. The app exports OTLP to the collector; the collector fans out to Jaeger (traces) and Prometheus (metrics).

Pin image tags in anything beyond a throwaway local setup (`latest` is used here for brevity).

## docker-compose.yml

```yaml
services:
  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    command: ["--config=/etc/otel-collector-config.yaml"]
    volumes:
      - ./otel-collector-config.yaml:/etc/otel-collector-config.yaml
    ports:
      - "4317:4317"   # OTLP gRPC
      - "4318:4318"   # OTLP HTTP
    depends_on:
      - jaeger
      - prometheus

  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686" # Jaeger UI

  prometheus:
    image: prom/prometheus:latest
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"   # Prometheus UI

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
```

## otel-collector-config.yaml

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch: {}

exporters:
  otlp/jaeger:
    endpoint: jaeger:4317
    tls:
      insecure: true
  prometheus:
    endpoint: 0.0.0.0:8889

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlp/jaeger]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

## prometheus.yml

```yaml
global:
  scrape_interval: 15s
scrape_configs:
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otel-collector:8889']
```

## App side (any SDK)

```
OTEL_SERVICE_NAME=my-service
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317   # gRPC
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
# or, for SDKs/exporters that default to HTTP (Node HTTP exporters, Java agent 2.x):
# OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4318
# OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
```

If the app itself runs inside the same compose network, use `http://otel-collector:4317` instead of `localhost`.

## Where to look

- Traces -> Jaeger UI: http://localhost:16686
- Metrics -> Prometheus: http://localhost:9090
- Dashboards -> Grafana: http://localhost:3000 (add Prometheus + Jaeger as data sources)
