# How to use - Distributed Tracing Diagnostics

**What it is.** Finds where a distributed trace breaks - missing spans, dropped context across HTTP/gRPC/messaging, unregistered tracers, missing propagators, context lost across threads/async, sampling and flush issues - and fixes propagation so a request is one connected trace. Works across .NET, Node.js, Python, Java/Kotlin, and Go, including mixed-language systems.

**When to reach for it.** Traces that don't link across services, missing spans, orphaned traces, or every call showing up as a new root trace.

**How to use it.** Point it at the service(s) on the broken path (paths or GitHub URLs). Example prompts:
- "My traces don't connect across services - why?"
- "Spans from my own ActivitySource don't show up."
- "Trace context is lost over Kafka between my Node producer and Python consumer."
- "My Go service starts a new trace for every downstream call."
- "Spring @Async work shows up as separate traces."

**What you get.** The trace path across services, a numbered list of breaks (file:line, category, fix), the fix code per service, and what to verify in your tracing UI.

**Get the best out of it.** Give it all the services involved - breaks happen at boundaries, often between two languages. For messaging, ask for the inject/extract code for both sides. Pairs with `opentelemetry-setup`.

**Won't do.** It can't read your live backend - confirm the fix in Jaeger/Tempo/your vendor after applying.
