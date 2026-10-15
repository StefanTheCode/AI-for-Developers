# How to use - Metrics Dashboard Generator

**What it is.** Generates Prometheus scrape config and a ready-to-import Grafana dashboard JSON for a service in any stack - RED panels (rate/errors/duration), runtime health (GC, heap, threads, event loop, goroutines), and your custom metrics. It detects whether you use OpenTelemetry, Micrometer (Spring Boot), prom-client, prometheus_client, or client_golang and uses the metric names that stack actually emits.

**When to reach for it.** Visualizing a service's metrics, or standing up a Grafana dashboard fast.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Generate a Grafana dashboard for this ASP.NET Core service."
- "Prometheus config plus RED panels by route for my Spring Boot app."
- "Dashboard for my Node service using prom-client, include event loop lag."
- "Add panels for my custom order metrics in this Go service."

**What you get.** What it detected, the scrape config, a complete dashboard JSON (with datasource and instance variables), and a list of metric/label names to verify.

**Get the best out of it.** Let it scan for your custom metrics so it builds panels for them. If you can, give it the output of your `/metrics` endpoint - real names beat conventions. Pairs with `opentelemetry-setup`.

**Won't do.** It can't read your live Prometheus - confirm metric names, and watch label cardinality (no user-id or raw-path labels).
