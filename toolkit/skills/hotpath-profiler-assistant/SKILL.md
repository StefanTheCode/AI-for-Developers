---
name: hotpath-profiler-assistant
description: Guide profiling an app in any stack to find the real hot paths before optimizing - which tool to use, how to capture under load, and how to read the results. Covers .NET (dotnet-counters, dotnet-trace, dotnet-gcdump, PerfView, VS/Rider profiler), Node.js (--cpu-prof, --inspect + DevTools, clinic.js, 0x), Python (py-spy, cProfile + snakeviz, scalene, pyinstrument), Java/Kotlin (async-profiler, JFR + JDK Mission Control), Go (pprof, go tool trace), plus perf/flame graphs. Use this skill whenever the user wants to profile, find bottlenecks, figure out what's slow, capture a CPU or wall-clock trace, read a flame graph, or asks "what's slow in my app / how do I profile this / where's the bottleneck / why is CPU at 100%". Always use this skill before micro-optimizing; it finds where time actually goes.
category: Performance
version: 1.0.0
---

# Hot-Path Profiler Assistant

Find where time and allocations actually go before optimizing anything - pick the right tool for the stack, capture a useful profile under realistic load, and interpret it. Stops premature optimization of cold code.

## Input - point it at your project / describe the symptom

Works on a target (a **project**, **repo**, or **GitHub URL**) for context, plus the symptom: slow endpoint, high CPU, latency spikes, throughput ceiling, memory growth. If the user already has a profile (flame graph SVG/HTML, `.nettrace`, `.cpuprofile`, `.jfr`, pprof file, py-spy/speedscope output), go straight to interpretation.

## Step 1 - detect the stack and runtime environment

| Manifest | Stack | Start with | CPU / wall profile | Memory / heap |
|---|---|---|---|---|
| `*.csproj` | .NET | `dotnet-counters` | `dotnet-trace`, PerfView, VS/Rider profiler | `dotnet-gcdump`, `dotnet-dump` |
| `package.json` | Node.js | clinic.js `doctor` | `node --cpu-prof`, `--inspect` + DevTools, clinic `flame`, 0x | heap snapshots, `--heap-prof`, clinic `heapprofiler` |
| `pyproject.toml`, `requirements*.txt` | Python | `py-spy top` | py-spy `record`, cProfile, pyinstrument | scalene, memray, tracemalloc |
| `pom.xml`, `build.gradle(.kts)` | JVM | JFR | async-profiler, JFR + JDK Mission Control | JFR allocation events, heap dump + Eclipse MAT |
| `go.mod` | Go | pprof `/debug/pprof` | pprof CPU profile, `go tool trace` | pprof heap/allocs |

Also establish **where it runs** (local, container, k8s, prod) - it decides whether you attach to a live process, run a sidecar, or reproduce locally.

## Universal workflow

1. **Classify the problem first** with cheap, live metrics: CPU-bound, waiting (I/O, locks, pool starvation), GC/memory, or downstream latency. Different classes need different profiles.
2. **Reproduce under realistic load** - drive the hot endpoint with a load tool (k6, bombardier, wrk, autocannon, locust, Gatling) while capturing. A single cold request profiles startup and JIT, not the hot path.
3. **Pick the profile type:**
   - **CPU (on-CPU sampling)** - "CPU is high / it's slow and busy".
   - **Wall-clock / off-CPU** - "it's slow but CPU is low" (waiting on I/O, locks, thread pool, event loop).
   - **Allocation** - GC time is high, or memory churns.
   - **Heap snapshot** - memory grows and doesn't come back (retention/leak).
4. **Capture 30-60 seconds** of steady state, with symbols/source maps available.
5. **Read it** (see below), pick the widest frame you own, fix it, re-measure, repeat.

## C# / .NET

- **`dotnet-counters`** - live, low-overhead metrics (CPU, GC, thread pool queue length, exceptions, requests). Start here to see *what kind* of problem it is.
- **`dotnet-trace`** - sampling/event trace of CPU time; cross-platform; open in Speedscope, PerfView, or Visual Studio.
- **`dotnet-gcdump` / `dotnet-dump`** - heap snapshots for leaks/retention; `dotnet-dump analyze` for thread pool starvation and deadlocks.
- **PerfView** (Windows) - deep ETW analysis, allocation tick, GC events.
- **VS / Rider (dotTrace, dotMemory) profilers** - IDE-integrated, friendly flame graphs.
```
dotnet-counters monitor -p <pid>
dotnet-trace collect -p <pid> --duration 00:00:30 --format Speedscope   # open in speedscope.app
dotnet-gcdump collect -p <pid>                                          # heap snapshot
```
A growing thread pool queue with low CPU = sync-over-async or blocking I/O (see `async-await-auditor`).

## Node.js

```
node --cpu-prof app.js                 # writes a .cpuprofile on exit; open in DevTools or speedscope
node --inspect app.js                  # attach Chrome DevTools -> Performance / Memory tabs
npx clinic doctor -- node app.js       # classifies: event-loop blocking, I/O, GC
npx clinic flame -- node app.js        # flame graph
npx autocannon -c 50 -d 30 http://localhost:3000/orders   # load while profiling
```
- High event-loop delay with high CPU = synchronous work on the main thread (JSON on big payloads, `*Sync` calls, regex backtracking, crypto). Fix by moving off the loop or chunking.
- Use `--enable-source-maps` / source maps so TypeScript frames are readable.

## Python

```
py-spy top --pid <pid>                                  # live view, no code changes
py-spy record -o profile.svg --pid <pid> --duration 30  # flame graph; --idle to include waiting threads
python -m cProfile -o out.prof app.py && snakeviz out.prof
scalene app.py                                          # per-line CPU (Python vs native) and memory
```
- py-spy is sampling and safe for production; cProfile is deterministic but adds overhead and distorts small functions.
- pyinstrument gives readable wall-clock call trees, including for async code.
- Check the GIL: CPU-bound code in threads won't scale - move to processes or native code.

## Java / Kotlin

```
jcmd <pid> JFR.start duration=60s filename=rec.jfr   # open in JDK Mission Control
asprof -d 30 -e cpu -f flame.html <pid>              # async-profiler CPU flame graph
asprof -d 30 -e wall -f wall.html <pid>              # wall-clock: waiting threads included
asprof -d 30 -e alloc -f alloc.html <pid>            # allocation flame graph
```
(Older async-profiler releases ship the launcher as `profiler.sh` instead of `asprof`.)
- JFR is built into the JDK and low overhead enough for production; look at hot methods, lock contention, GC pauses, and allocation by stack.
- Profile after warm-up - early samples are interpreter/JIT compilation.

## Go

```go
import _ "net/http/pprof"   // exposes /debug/pprof on the default mux (serve it on an internal port)
```
```
go tool pprof -http=:8080 http://localhost:6060/debug/pprof/profile?seconds=30   # CPU
go tool pprof -http=:8080 http://localhost:6060/debug/pprof/heap                 # heap (inuse/alloc)
go test -bench . -cpuprofile cpu.out -memprofile mem.out ./pkg && go tool pprof -http=:8080 cpu.out
curl -o trace.out "http://localhost:6060/debug/pprof/trace?seconds=5" && go tool trace trace.out
```
- Enable block and mutex profiles (`runtime.SetBlockProfileRate`, `runtime.SetMutexProfileFraction`) for "slow but CPU is low".
- `go tool trace` shows scheduler latency, GC, and goroutine blocking over time.

## Any native/Linux process

`perf record -g -p <pid>` plus Brendan Gregg's FlameGraph scripts, or `cargo flamegraph` for Rust. JIT runtimes need perf map support (e.g. `DOTNET_PerfMapEnabled=1`, `node --perf-basic-prof`) for readable frames.

## Reading it

- **Flame graph width = time** (or allocations). The widest frames are where you optimize; ignore narrow ones. Height is call depth, not cost.
- **Self time vs total time** - a wide frame with little self time is just a caller; follow it down to where the work actually happens.
- Separate **CPU-bound** (busy frames) from **wait** (I/O, locks, async) - different fixes.
- Check **allocations and GC** alongside CPU - GC time can masquerade as slowness (`gc-pressure-auditor`).
- Look for **frames you own** - framework/runtime frames are usually symptoms; find the first frame of your code beneath them.
- Confirm the suspected method is actually hot before touching it.

## Output format

```
## Detected
<stack, runtime, environment, symptom class>

## Capture plan
<exact commands: live metrics, profile type, load to drive, duration>

## Interpretation (when a profile is provided)
1. <frame/function> - <share of time/allocations> - <why it's hot> -> <next step>

## Next
<hand-off: benchmark-setup to lock in a micro-benchmark, memory-allocation-analyzer, gc-pressure-auditor, orm-query-optimizer, async-await-auditor>
```

## Principles

- **Profile first, optimize second.** Most "obvious" bottlenecks are wrong guesses.
- Measure under realistic load, after warm-up, not a single cold request.
- Optimize the widest frame you own, re-measure, repeat - don't shotgun.
- Prefer sampling profilers in production; instrumenting profilers distort small, hot functions.

## How to use it & best prompts

"What's slow in my app", "where's the bottleneck in this endpoint", "how do I capture a CPU trace", "profile my Node service under load", "py-spy or cProfile?", "set up async-profiler in our pods", "read this flame graph / pprof / trace". Feeds into `benchmark-setup`, `memory-allocation-analyzer`, `gc-pressure-auditor`.
