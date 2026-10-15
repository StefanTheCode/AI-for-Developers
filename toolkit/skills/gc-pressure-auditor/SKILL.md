---
name: gc-pressure-auditor
description: Diagnose garbage-collection and memory pressure in a service on any managed runtime and recommend fixes - .NET GC (Server vs Workstation, LOH, Gen2, DATAS, GCHeapHardLimit), JVM GC (G1, ZGC, heap sizing, humongous objects, allocation rate), Go GC (GOGC, GOMEMLIMIT, escape analysis), V8/Node heap (old space, scavenges, --max-old-space-size), Python memory (refcounting + cyclic gc, memory growth, fragmentation). Use this skill whenever the user reports GC pauses, high CPU in GC, memory growth, OOM kills in containers, LOH fragmentation, Gen2 collections, long G1/ZGC pauses, "JavaScript heap out of memory", RSS that never goes down, or asks "why is the GC running so much / reduce GC pauses / tune GC / tune GOGC / heap size for my container". Always use this skill for GC-pressure diagnosis; it covers config, allocation sources, retention, and pooling.
category: Performance
version: 1.0.0
---

# GC Pressure Auditor

Diagnose why the garbage collector (or the memory manager) is working hard and recommend concrete fixes - runtime config, allocation reduction, retention fixes, and pooling - based on data, not guesses.

## Input - point it at your project / paste the symptoms

Works on a target (a **project**, **repo**, or **GitHub URL**) and/or data the user shares: GC counters, GC logs, `gctrace` output, heap snapshots, RSS graphs, container limits, OOM events. If no data is available, tell the user exactly what to capture first (see per-runtime "Measure") and audit config and code in the meantime.

## Step 1 - detect the runtime and where config lives

| Manifest | Runtime | Where GC config lives |
|---|---|---|
| `*.csproj` | .NET CLR | `.csproj` properties, `runtimeconfig.json`, `DOTNET_*` env vars |
| `pom.xml`, `build.gradle(.kts)` | JVM | JVM flags in Dockerfile/`JAVA_TOOL_OPTIONS`/start scripts |
| `go.mod` | Go | `GOGC`, `GOMEMLIMIT` env vars, `debug.SetGCPercent` / `debug.SetMemoryLimit` |
| `package.json` | Node (V8) | `node` flags / `NODE_OPTIONS` |
| `pyproject.toml`, `requirements*.txt` | CPython | `gc` module calls, server config (gunicorn/uvicorn workers), allocator env vars |

Then check deployment: container memory/CPU limits (Dockerfile, k8s manifests, Helm values). Most GC misbehavior in containers is a runtime that doesn't know, or isn't told, its real limit.

## Universal diagnosis

1. **Classify the symptom:** high **allocation rate** (frequent young-gen collections, CPU in GC), **retention/leak** (heap after full GC keeps growing), **large-object churn** (big buffers, periodic long pauses), or **mis-sized heap** (OOM kill below the runtime's own limit, or constant GC near a too-small limit).
2. **Allocation rate is the root cause most of the time.** Fewer, smaller, shorter-lived allocations beat any GC flag. Find the sources with an allocation profiler (`memory-allocation-analyzer`).
3. **Mid-life objects are the expensive ones** - objects that survive young collections and then die (request-scoped data parked in caches, big graphs held across awaits, unbounded queues).
4. **Large buffers:** pool or stream instead of allocating per request; chunk large payloads.
5. **Unbounded caches and collections** are the usual leak: add size limits/TTL/eviction.
6. **Size the heap to the container:** leave headroom for non-heap memory (thread stacks, native buffers, code, metadata).
7. **Tune config last**, one change at a time, and re-measure.

## .NET

**Measure:** `dotnet-counters monitor -p <pid> --counters System.Runtime` (GC heap size, gen 0/1/2 counts, % time in GC, allocation rate, LOH size); `dotnet-trace` with the GC providers; `dotnet-gcdump` for retention.
Config sweep: `grep -rn "ServerGarbageCollection\|ConcurrentGarbageCollection\|GCHeapHardLimit\|GarbageCollectionAdaptationMode" <target>` plus `.csproj`/`runtimeconfig.json`.

1. **Server vs Workstation GC** - a throughput server should use **Server GC** (`<ServerGarbageCollection>true</ServerGarbageCollection>`; ASP.NET Core defaults to it). Workstation GC on a busy server = avoidable pauses.
2. **Concurrent (background) GC** - keep it on for latency-sensitive services.
3. **DATAS** (dynamic adaptation of Server GC heap count) - opt-in on .NET 8, on by default with Server GC on .NET 9+. Reduces memory for bursty or small-container workloads; if throughput regresses, compare with it disabled.
4. **Allocation rate** - high Gen0 churn means too many short-lived allocations; trace the sources.
5. **Gen2 churn** - objects surviving to Gen2 then dying: caching done wrong or large graphs held too long.
6. **Large Object Heap (LOH)** - arrays/strings of 85,000 bytes or more go to the LOH, which is collected with Gen2 and can fragment. Pool large buffers (`ArrayPool<T>.Shared`, `RecyclableMemoryStream`), stream instead of buffering, chunk large payloads.
7. **Pinning / fragmentation** - long-lived pinned buffers fragment the heap; use the Pinned Object Heap (`GC.AllocateArray(..., pinned: true)`) for buffers that must stay pinned.
8. **Containers** - the GC honors cgroup limits; set `GCHeapHardLimit`/`GCHeapHardLimitPercent` when other memory in the process needs explicit headroom.

## JVM (Java / Kotlin)

**Measure:** unified GC logs `-Xlog:gc*:file=gc.log`, JFR (`jcmd <pid> JFR.start`) for GC pauses and allocation by stack, async-profiler in alloc mode, `jcmd <pid> GC.heap_info`, heap dump + Eclipse MAT for retention.

1. **Collector choice** - G1 is the default and right for most services. For large heaps with strict tail-latency needs, **ZGC** (`-XX:+UseZGC`; generational ZGC is available from JDK 21 and the default mode in newer JDKs). Parallel GC for pure batch throughput.
2. **Heap sizing in containers** - prefer `-XX:MaxRAMPercentage` (e.g. 70-80%) over a hard-coded `-Xmx` so the heap follows the container limit; leave room for metaspace, thread stacks, direct buffers. Set `-Xms` equal to `-Xmx` for services where predictable footprint matters.
3. **Humongous allocations (G1)** - objects of at least half a region go straight to humongous regions (the JVM's LOH). Pool/stream large arrays or increase `-XX:G1HeapRegionSize`.
4. **Pause goals** - `-XX:MaxGCPauseMillis` is a target, not a guarantee; tightening it trades throughput.
5. **Allocation hotspots** - boxing in collections/streams, string concatenation in loops, per-request `ObjectMapper`/formatter creation.
6. **Direct/off-heap memory** - Netty and NIO buffers live outside the heap; OOM kills with a "healthy" heap often come from here (`-XX:MaxDirectMemorySize`, Native Memory Tracking `-XX:NativeMemoryTracking=summary`).

## Go

**Measure:** `GODEBUG=gctrace=1` (per-GC heap sizes and pause times), `runtime/metrics` or `runtime.ReadMemStats`, `go tool pprof` heap profile (`-sample_index=alloc_space` for allocation volume, `inuse_space` for retention).

1. **GOGC** (default 100) - heap grows to live heap x (1 + GOGC/100) before the next cycle. Raising it trades memory for less GC CPU.
2. **GOMEMLIMIT** (Go 1.19+) - a soft memory limit; set it to roughly 90% of the container limit so the GC works harder near the limit instead of getting OOM-killed. Combine with a higher GOGC (or `GOGC=off` with care) for steady memory use.
3. **Escape analysis** - `go build -gcflags=-m ./...` shows what escapes to the heap. Common causes: returning pointers to locals, storing in interfaces, closures capturing variables, slices whose size isn't known at compile time.
4. **Reuse** - `sync.Pool` for short-lived buffers on hot paths; preallocate slices/maps with capacity; `strings.Builder`/`bytes.Buffer` reuse.
5. **Retention** - goroutine leaks hold their stacks and references; subslices of huge arrays keep the whole array alive (copy the part you need).

## Node.js (V8)

**Measure:** `node --trace-gc`, `process.memoryUsage()` (heapUsed vs rss vs external/arrayBuffers), `PerformanceObserver` for `gc` entries, heap snapshots (`--heapsnapshot-signal`, Chrome DevTools via `--inspect`, `v8.writeHeapSnapshot()`), `--heap-prof` for allocation sampling, clinic.js `heapprofiler`.

1. **Old-space limit** - "JavaScript heap out of memory" or constant mark-sweep near the limit: set `--max-old-space-size` (MB) to roughly 75% of the container limit, leaving room for Buffers and native memory.
2. **Young-generation churn** - many scavenges from high allocation rates; reduce allocations first. `--max-semi-space-size` can reduce promotion for allocation-heavy workloads - measure before keeping it.
3. **Leaks** - module-level Maps/arrays used as caches, listeners never removed, closures capturing large objects, timers never cleared. Compare two heap snapshots taken under load and look at retainers.
4. **Buffers** - large `Buffer`s live outside the V8 heap (`external`/`arrayBuffers`); stream instead of `readFile` + concat for big payloads.

## Python (CPython)

**Measure:** RSS over time, `tracemalloc` snapshots (compare two), memray for native + Python allocations, `gc.get_stats()` / `gc.callbacks` for collection counts and pause time, objgraph for growth by type, scalene for per-line memory.

1. **Refcounting frees most objects immediately**; the cyclic `gc` only handles reference cycles. Frequent gc runs mean many container allocations; long pauses mean large tracked object graphs.
2. **Cycles** - objects referencing each other (parent/child links, exceptions holding tracebacks and frames) wait for the cyclic collector. Break cycles or use `weakref`.
3. **Thresholds** - `gc.get_threshold()` / `gc.set_threshold()` tune collection frequency; raise only with measurements. Don't `gc.disable()` in long-running services.
4. **Pre-fork servers** (gunicorn with preload) - `gc.freeze()` after loading the app moves startup objects out of collection, reducing copy-on-write memory growth in workers.
5. **"Memory never goes down"** - often allocator fragmentation, not a leak: pymalloc arenas and glibc malloc keep freed memory. Check whether tracemalloc totals are flat while RSS grows; mitigations include `MALLOC_ARENA_MAX`, jemalloc, or recycling workers (`max_requests`).
6. **Leaks** - unbounded module-level caches, `functools.lru_cache` without `maxsize` on methods (it also keeps `self` alive), growing lists of results.

## Output format

```
## Detected
<runtime + version, collector/config in use, container limits>

## Diagnosis
<symptom class: allocation rate / retention / large objects / heap sizing - and the evidence>

## Findings
1. [config] <file/setting> - <problem> -> <fix>
2. [allocation] <file:line> - <source> -> <fix>
3. [retention] <type/cache> - <why it grows> -> <fix>

## Recommended config
<exact settings/flags/env vars for this runtime>

## Measure next
<what to capture to confirm, and what "fixed" looks like>
```

## Principles

- **GC pressure is an allocation problem first.** Config tuning helps, but reducing what you allocate helps more.
- Diagnose with data (counters, GC logs, gctrace, heap snapshots) - don't tune blind.
- Separate allocation rate from retention: they have different fixes.
- Large objects and mis-sized heaps are the usual culprits for big, periodic pauses and OOM kills.

## How to use it & best prompts

"Why is GC eating CPU", "reduce GC pauses in this service", "LOH keeps growing", "should I use Server GC", "G1 or ZGC for this service", "tune GOGC/GOMEMLIMIT for our pods", "JavaScript heap out of memory", "Python worker memory keeps growing". Pairs with `memory-allocation-analyzer` and `hotpath-profiler-assistant`.
