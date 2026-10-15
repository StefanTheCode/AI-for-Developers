# How to use - Serialization Optimizer

**What it is.** Optimizes JSON serialization in any stack - reused serializer config, reflection-free serialization (source generation, codegen, schema-compiled serializers), streaming, payload trimming, and the small mistakes (a new serializer per call) that cost a lot. It detects your stack first: System.Text.Json (.NET), JSON.stringify / fast-json-stringify / Fastify schemas (Node), orjson / msgspec / pydantic v2 (Python), Jackson or kotlinx.serialization (Java/Kotlin), encoding/json vs goccy/go-json, sonic, easyjson (Go).

**When to reach for it.** Slow or allocation-heavy JSON, high-throughput APIs, large payloads blocking an event loop, or AOT/startup-sensitive apps.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Add System.Text.Json source generation to this API."
- "Why does serializing this allocate so much?"
- "Switch this FastAPI service to orjson / pydantic `model_dump_json`."
- "Add Fastify response schemas so serialization is faster."
- "Is my Jackson ObjectMapper set up right?"
- "Make this Go handler's JSON encoding faster."

**What you get.** Findings ranked by impact with file and line, optimized serializer setup and hot-path code ready to paste, and a benchmark to confirm the gain.

**Get the best out of it.** The biggest wins are reusing serializer config and removing runtime reflection - ask for those first. Point it at the hottest endpoints or consumers. Stream large payloads. Verify with `benchmark-setup` (e.g. BenchmarkDotNet `[MemoryDiagnoser]`, JMH, pytest-benchmark, Go `testing.B` with `-benchmem`).

**Won't do.** It won't hand-roll converters the serializer already handles, and it won't micro-optimize a cold serialization path or swap libraries without a measured reason.
