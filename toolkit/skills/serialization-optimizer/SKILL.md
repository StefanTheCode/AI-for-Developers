---
name: serialization-optimizer
description: Optimize JSON serialization in any stack - System.Text.Json source generation and options reuse in .NET, JSON.stringify vs fast-json-stringify and Fastify response schemas in Node, orjson / msgspec / pydantic v2 in Python, Jackson ObjectMapper reuse and streaming in Java/Kotlin (plus kotlinx.serialization), encoding/json vs goccy/go-json, sonic, or easyjson in Go. Covers reflection and allocation overhead, reused serializer config, schema/codegen-based serializers, streaming large payloads, payload trimming, and when a binary format fits better. Use this skill whenever the user has slow or allocation-heavy serialization, high-throughput JSON APIs, AOT/startup-sensitive apps, or mentions System.Text.Json, JsonSerializer, source generation, Jackson, ObjectMapper, orjson, msgspec, pydantic model_dump_json, fast-json-stringify, encoding/json, or asks "how do I speed up JSON / optimize serialization / reduce JSON allocations". Always use this skill for serialization performance; it applies the high-impact fixes first.
category: Performance
version: 1.0.0
---

# Serialization Optimizer

Make JSON serialization faster and leaner - reused serializer configuration, schema/codegen-based serializers, streaming, and the small mistakes that cost a lot. The wins are the same in every stack; the libraries differ.

## Input - point it at your code
Works on a target: a **file**, **folder**, **project/repo**, or **GitHub URL**. Find serializer usage, where config objects are created, and the hottest paths (API responses, message consumers, cache writes). Report which files you scanned.

## Step 1 - detect the stack and serializer

| Manifest | Look for | Grep |
|---|---|---|
| `*.csproj` | `System.Text.Json` (in-box), `Newtonsoft.Json` | `JsonSerializer\.\|JsonSerializerOptions\|JsonConvert\.` |
| `package.json` | `fastify`, `fast-json-stringify`, `superjson` | `JSON.stringify\|JSON.parse\|fast-json-stringify` |
| `pyproject.toml`, `requirements*.txt` | `orjson`, `msgspec`, `pydantic`, `ujson` | `json.dumps\|json.loads\|model_dump\|orjson\.` |
| `pom.xml`, `build.gradle(.kts)` | `jackson-databind`, `kotlinx-serialization-json`, `gson` | `new ObjectMapper\|ObjectMapper()\|Json.encodeToString` |
| `go.mod` | `encoding/json` (stdlib), `goccy/go-json`, `bytedance/sonic`, `mailru/easyjson` | `json.Marshal\|json.Unmarshal\|json.NewEncoder` |

## Universal checklist (do these first)
1. **Reuse serializer configuration.** Building a serializer/options/mapper per call throws away its metadata cache - often the single biggest win.
2. **Avoid runtime reflection on hot paths** - source generation, codegen, or schema-compiled serializers.
3. **Stream, don't buffer.** Write directly to the response/output stream; don't build a giant string and then write it. Parse large inputs incrementally.
4. **Work in UTF-8 bytes** where the library allows it; skip string round-trips.
5. **Trim the payload.** Don't serialize nulls/defaults you don't need, ignore internal fields, and send DTOs - never ORM entities (lazy relations, cycles, over-fetching).
6. **Double serialization** - `serialize(deserialize(x))` passthroughs, or serializing to a string and then encoding to bytes again.
7. **Custom converters only when needed** - and keep them allocation-light.
8. **Consider a binary format** (Protobuf, MessagePack) for internal high-volume service-to-service or cache traffic when JSON is the measured bottleneck.

## C# / .NET (System.Text.Json)
1. **Reuse `JsonSerializerOptions`** - create one static instance (or use `JsonSerializerOptions.Web` / the ASP.NET Core-configured instance). A new options object per call is a common big loss.
2. **Source generation** - avoid reflection with a `JsonSerializerContext`:
```csharp
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(List<Order>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AppJsonContext : JsonSerializerContext { }

var json = JsonSerializer.Serialize(order, AppJsonContext.Default.Order);
// ASP.NET Core: builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));
```
Faster startup, lower allocations, required for Native AOT and trimming.
3. **Stream** - `JsonSerializer.SerializeAsync(stream, ...)` / `DeserializeAsync`, `DeserializeAsyncEnumerable` for large arrays, instead of serializing to a string then writing.
4. **`Utf8JsonWriter` / `Utf8JsonReader`** for the hottest custom paths - work on UTF-8 bytes directly; `SerializeToUtf8Bytes` beats `Serialize` + `Encoding.UTF8.GetBytes`.
5. **Trim** - `[JsonIgnore]`, `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`.
6. **Newtonsoft.Json** - migrating hot paths to System.Text.Json is usually worthwhile; check converter and behavior differences (case sensitivity, reference handling) first.

## TypeScript / Node
- `JSON.stringify` / `JSON.parse` are native and fast for most payloads; the real cost is **blocking the event loop** on large payloads. Stream large arrays (e.g. NDJSON, `stream-json` for parsing) or move heavy work off the main thread.
- **Schema-compiled serialization:** `fast-json-stringify` compiles a JSON Schema into a serializer; Fastify uses it automatically when you declare `response` schemas per route (also strips undeclared fields - a security bonus).
- Avoid `JSON.parse(JSON.stringify(x))` for cloning - use `structuredClone`.
- Avoid serializing class instances with getters/ORM proxies; map to plain DTOs first.
- `superjson` and similar add type fidelity (Date, Map) at a cost - don't use them on hot paths that don't need it.

## Python
- `json` stdlib is slow for large payloads. **orjson** (returns `bytes`, natively handles `datetime`, `dataclass`, `UUID`, numpy) or **msgspec** (`msgspec.json.encode/decode`, typed `Struct` decoding with validation) are typically much faster.
- **pydantic v2:** use `model.model_dump_json()` and `Model.model_validate_json(raw)` (the Rust core parses directly) instead of `json.dumps(model.model_dump())` / `Model(**json.loads(raw))`; build a `TypeAdapter` once and reuse it.
- FastAPI: return the response model or use `ORJSONResponse`; avoid double validation (validating the model and again via `response_model` on large lists) on hot endpoints.
- Encode once to bytes; don't `.decode()` to `str` just to write bytes again.

## Java / Kotlin (Jackson)
- **Reuse `ObjectMapper`** - it is thread-safe after configuration and expensive to build. Prefer a single configured bean; derive `ObjectReader` / `ObjectWriter` (`mapper.readerFor(Order.class)`) for hot types.
- **Stream** large payloads with `JsonGenerator` / `JsonParser` or `SequenceWriter` instead of building trees (`JsonNode`) in memory.
- **Trim** - `@JsonInclude(JsonInclude.Include.NON_NULL)`, `@JsonIgnore`, DTO projections instead of entities (Hibernate proxies and bidirectional relations cause lazy loads and cycles).
- The **Blackbird** module (successor to Afterburner on modern JDKs) can speed up databind; measure first.
- Kotlin: `kotlinx.serialization` is compile-time (no reflection) and a good fit for Ktor/multiplatform; with Jackson, register `jackson-module-kotlin` once on the shared mapper.

## Go
- `encoding/json` is reflection-based; it caches type info per type, so reuse is automatic, but it allocates heavily.
- **Stream**: `json.NewEncoder(w).Encode(v)` straight to `http.ResponseWriter`; `json.NewDecoder(r.Body)` for request bodies (and `Token()` for huge arrays).
- Faster alternatives: `goccy/go-json` (drop-in compatible API), `bytedance/sonic` (JIT, amd64/arm64), `json-iterator/go`, or codegen with `mailru/easyjson`. Verify behavior parity with your tests before switching.
- `json.RawMessage` to pass through sub-documents without decoding them.
- Avoid `map[string]interface{}` on hot paths - decode into typed structs.

## Output
```
## Summary
<stack + serializer detected; where the time/allocations go>

## Findings
🔴 [Options per call] <file:line> - <cost> -> <fix>
🟡 [Buffering large response] ...

## Optimized code
<serializer setup + rewritten hot paths, ready to paste>

## Measure
<benchmark to run before/after - see benchmark-setup>
```

## Principles
- **Config reuse + reflection-free serialization** are most of the wins - do those first.
- Stream large payloads; don't materialize giant strings.
- Measure allocations and throughput before and after - serialization is often an allocation story.
- Don't hand-roll converters or switch libraries for a cold path.
- Serialize DTOs, not domain/ORM objects.

## How to use it & best prompts
"Speed up my JSON serialization", "add System.Text.Json source generation", "why does serializing this allocate so much", "switch this FastAPI service to orjson", "add Fastify response schemas", "is my ObjectMapper set up right", "make this Go handler's JSON faster", "stream this large JSON response". Pairs with `benchmark-setup` and `caching-strategy-setup`.
