---
name: benchmark-setup
description: Set up correct micro-benchmarks in any stack and interpret the results - BenchmarkDotNet (.NET), JMH (Java/Kotlin), pytest-benchmark / pyperf (Python), Vitest bench / tinybench / mitata (JavaScript/TypeScript), Go testing.B + benchstat, Rust criterion. Use this skill whenever the user wants to benchmark code, measure performance, compare two implementations or functions, set up a benchmark harness, or asks "which is faster", "benchmark these two functions", "how do I benchmark this", "is this optimization worth it", or "interpret these benchmark results". Always use this skill for micro-benchmarking requests; it writes correct benchmarks, avoids the classic pitfalls, and explains the numbers.
category: Performance
---

# Benchmark Setup

Write correct micro-benchmarks with the right tool for the stack, and read the results properly - so "which is faster" is answered with data, not vibes.

## Input - point it at your code

Works on a **file**, **folder**, **project**, or **GitHub URL**. Identify the functions/implementations to compare. If the user pasted results, go straight to interpretation.

## Step 1 - detect the stack, pick the tool

| Manifest | Default tool | Alternative |
|---|---|---|
| `*.csproj` | BenchmarkDotNet | - |
| `pom.xml` / `build.gradle(.kts)` | JMH | kotlinx-benchmark (Kotlin Multiplatform) |
| `pyproject.toml` / `requirements*.txt` | pytest-benchmark (if pytest is used) | pyperf (standalone, more rigorous) |
| `package.json` | Vitest bench (if Vitest is used) | tinybench, mitata |
| `go.mod` | `testing.B` + `benchstat` | - |
| `Cargo.toml` | criterion | divan |

If the project already uses a benchmark tool, use that one.

## Universal rules (every stack)

1. **Optimized build only.** Never benchmark Debug / dev mode / with a debugger, profiler, or coverage attached.
2. **Warmup.** JIT runtimes (.NET, JVM, V8, PyPy) need warmup before measuring. Use the tool's harness; never hand-roll a stopwatch loop.
3. **Defeat dead-code elimination.** Return or consume the result (BenchmarkDotNet return value, JMH `Blackhole`, `black_box` in Rust, a package-level sink in Go).
4. **Setup outside the measured region.** Build inputs once in the setup hook.
5. **Always have a baseline.** Compare against the current implementation, not against nothing.
6. **Measure allocations**, not just time. A faster method that allocates 10x more can lose under real load.
7. **Respect noise.** Run multiple iterations/processes, read the error/stddev, and treat differences inside the noise as "no difference". Close other heavy apps; on laptops, plug in power.
8. **Realistic inputs.** Size and shape matching production; parameterize sizes (small / typical / large) when the algorithm's behavior changes with size.
9. **One variable at a time.** Keep benchmarks isolated and deterministic (fixed seeds).
10. **Confirm the path is hot first.** Profile before micro-optimizing code that isn't on the critical path.

## C# / .NET - BenchmarkDotNet

```
dotnet add package BenchmarkDotNet
```
```csharp
[MemoryDiagnoser]                                  // allocations matter as much as time
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class StringJoinBenchmarks
{
    private string[] _items = default!;

    [Params(10, 1000)] public int N;

    [GlobalSetup]
    public void Setup() => _items = Enumerable.Range(0, N).Select(i => i.ToString()).ToArray();

    [Benchmark(Baseline = true)]
    public string Concat() { var s = ""; foreach (var i in _items) s += i; return s; }

    [Benchmark]
    public string Builder() { var sb = new StringBuilder(); foreach (var i in _items) sb.Append(i); return sb.ToString(); }

    [Benchmark]
    public string Join() => string.Join("", _items);
}
// Program.cs:  BenchmarkRunner.Run<StringJoinBenchmarks>();
```
Run: `dotnet run -c Release`. Pitfalls: Debug build or no `BenchmarkRunner`; setup work inside `[Benchmark]` (use `[GlobalSetup]`); not returning the result; no `[Baseline]`.
Reading: **Mean** + **Error/StdDev**, **Ratio** vs baseline, **Gen0/1/2 + Allocated** for GC pressure.

## Java / Kotlin - JMH

Maven: generate a module from the `org.openjdk.jmh:jmh-java-benchmark-archetype` archetype. Gradle: the `me.champeau.jmh` plugin, benchmarks in `src/jmh/java`.
```java
@State(Scope.Benchmark)
@BenchmarkMode(Mode.AverageTime)
@OutputTimeUnit(TimeUnit.NANOSECONDS)
@Warmup(iterations = 5) @Measurement(iterations = 5) @Fork(2)
public class JoinBenchmark {
    @Param({"10", "1000"}) int n;
    List<String> items;

    @Setup public void setup() { items = IntStream.range(0, n).mapToObj(Integer::toString).toList(); }

    @Benchmark public String join() { return String.join("", items); }

    @Benchmark public void builder(Blackhole bh) {
        var sb = new StringBuilder(); for (var s : items) sb.append(s); bh.consume(sb.toString());
    }
}
```
Run the benchmarks jar (`java -jar target/benchmarks.jar`) or `./gradlew jmh`. Add `-prof gc` for allocation rate (`gc.alloc.rate.norm` = bytes/op). Never run JMH from the IDE's normal test runner; keep `@Fork` >= 1.

## Python - pytest-benchmark / pyperf

```python
# test_bench.py  (pip install pytest-benchmark)
def concat(items): return "".join(items)

def test_join(benchmark):
    items = [str(i) for i in range(1000)]          # setup outside the measured call
    result = benchmark(concat, items)
    assert result
```
Run: `pytest --benchmark-only`; save and compare runs with `--benchmark-autosave` and `--benchmark-compare`.
pyperf for standalone, process-isolated runs: `python -m pyperf timeit -s "items=[str(i) for i in range(1000)]" "''.join(items)" -o new.json`, then `python -m pyperf compare_to old.json new.json`. Allocations: `tracemalloc` or memray. Run with coverage and debuggers off.

## JavaScript / TypeScript - Vitest bench / tinybench / mitata

```ts
// join.bench.ts
import { bench, describe } from 'vitest';
const items = Array.from({ length: 1000 }, (_, i) => String(i));

describe('join', () => {
  bench('Array.join', () => { items.join(''); });
  bench('concat loop', () => { let s = ''; for (const i of items) s += i; });
});
```
Run: `npx vitest bench`. Without Vitest, tinybench (`new Bench()`, `bench.add(...)`, `await bench.run()`, `console.table(bench.table())`) or mitata (`bench(...)`, `await run()`). Run against the built output or with production settings, no `--inspect`. V8 aggressively optimizes and inlines - make results observable and avoid constant inputs the optimizer can fold away.

## Go - testing.B + benchstat

```go
func BenchmarkJoin(b *testing.B) {
    items := make([]string, 1000)
    for i := range items { items[i] = strconv.Itoa(i) }
    b.ReportAllocs()
    b.ResetTimer()                       // exclude setup
    for i := 0; i < b.N; i++ {
        sink = strings.Join(items, "")
    }
}
var sink string                          // package-level sink defeats elimination
```
On Go 1.24+ prefer `for b.Loop() { ... }` (handles timer reset and keeps results alive). Run: `go test -bench=Join -benchmem -count=10 > old.txt`, change the code, repeat into `new.txt`, then `benchstat old.txt new.txt` (`golang.org/x/perf/cmd/benchstat`). Trust only deltas benchstat reports as significant.

## Rust - criterion

```toml
[dev-dependencies]
criterion = "<current version>"

[[bench]]
name = "join"
harness = false
```
```rust
use criterion::{criterion_group, criterion_main, Criterion};
use std::hint::black_box;

fn bench_join(c: &mut Criterion) {
    let items: Vec<String> = (0..1000).map(|i| i.to_string()).collect();
    c.bench_function("join", |b| b.iter(|| black_box(&items).concat()));
}
criterion_group!(benches, bench_join);
criterion_main!(benches);
```
Run: `cargo bench` (optimized profile). Criterion compares against the previous run automatically; use `--save-baseline main` and `--baseline main` for explicit comparisons.

## Reading the results (any tool)

- **Central value** (mean/median, ns/op or ops/sec) only matters next to its **error/stddev/confidence interval**.
- **Relative** comparison to the baseline is the headline (ratio, % change, benchstat delta).
- **Allocations / bytes per op** are often the real story for server workloads.
- If two results overlap within the noise, report "no meaningful difference" - don't declare a winner.

## Output format

```
## Setup
<stack + tool, install command, benchmark file(s), run command>

## Results / interpretation
<table or summary: baseline vs candidates, time + allocations, noise>

## Verdict
<which to use and why, or "no meaningful difference">
```
