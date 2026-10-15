---
name: memory-allocation-analyzer
description: Find unnecessary heap allocations on hot paths in any stack and rewrite them to reduce GC/memory pressure - C#/.NET (LINQ in loops, boxing, closures, string concatenation, params arrays, async state machines, Span), TypeScript/JavaScript (intermediate arrays from map/filter chains, spreads in loops, per-call closures, JSON clone round-trips), Python (materialized lists, string concatenation, missing __slots__, copies), Java/Kotlin (autoboxing, streams in hot loops, String concatenation, regex recompilation, unsized collections), Go (heap escapes, interface boxing, fmt.Sprintf, []byte/string conversions, append growth). Use this skill whenever the user wants to reduce allocations, cut GC pressure, find boxing/closures/escapes, optimize a hot path's memory, or asks "why does this allocate so much / reduce garbage / why does this escape to the heap". Always use this skill for allocation-reduction requests; it runs a fixed checklist and rewrites the code.
category: Performance
version: 1.0.0
---

# Memory Allocation Analyzer

Spot avoidable heap allocations on hot paths and rewrite them to cut GC pressure - without sacrificing readability where it doesn't matter.

## Input - point it at your code

Works on a target: a **file**, **folder**, **project**, or **GitHub URL** (clone first: `git clone --depth 1 <url> /tmp/repo`). Focus on hot paths: request handlers, tight loops, serialization, parsing, per-message processing. If the user has a profile or allocation report, start from its top frames; otherwise ask which paths are hot or confirm with `hotpath-profiler-assistant`.

## Step 1 - detect the stack

| Manifest | Stack | Measure allocations with |
|---|---|---|
| `*.csproj` | C# / .NET | BenchmarkDotNet `[MemoryDiagnoser]`, `dotnet-counters` allocation rate, `dotnet-trace` |
| `package.json` | TS / JS (V8) | `--heap-prof`, DevTools allocation sampling, `process.memoryUsage()` deltas |
| `pyproject.toml`, `requirements*.txt` | Python | `tracemalloc`, memray, scalene |
| `pom.xml`, `build.gradle(.kts)` | Java / Kotlin | JMH with `-prof gc`, JFR allocation events, async-profiler alloc mode |
| `go.mod` | Go | `go test -bench . -benchmem`, pprof `-sample_index=alloc_space`, `go build -gcflags=-m` |

Find candidates, e.g.:
```
# .NET
grep -rn "\.Select(\|\.Where(\|\.ToList()\|+ \"\|string.Format\|new \[\]" --include=*.cs <target> | grep -v "/obj/\|/bin/"
# TS / JS
grep -rnE "\.(map|filter|reduce)\(.*\)\.(map|filter)|\.\.\.|JSON\.parse\(JSON\.stringify" --include=*.{ts,js} <target> | grep -v node_modules
# Python
grep -rnE "\+= ['\"]|\[.* for .* in .*\]|copy\.deepcopy|\.copy\(\)" --include=*.py <target>
# Java / Kotlin
grep -rnE "\.stream\(\)|String\.format|Pattern\.compile|\.split\(|new ObjectMapper" <target>/src
# Go
go build -gcflags=-m ./... 2>&1 | grep "escapes to heap"
grep -rn "fmt\.Sprintf\|\[\]byte(\|string(" --include=*.go <target>
```

## Universal allocation checklist

1. **Intermediate collections in hot loops** - chained transforms that materialize a new list/array at each step. Fuse into one pass or iterate lazily.
2. **Boxing / wrapping** - value types or primitives turned into heap objects (generics erased to objects, interfaces, `Integer`, `any`). Keep primitives primitive.
3. **Closures** - lambdas capturing locals allocate per call. Hoist them, make them static, or pass state explicitly.
4. **String building** - concatenation in loops creates a new string each step. Use a builder/join.
5. **Hidden per-call allocations** - varargs arrays, defensive copies, re-created regexes/formatters/serializers, `new` of something that could be static or pooled.
6. **Unsized growth** - lists/maps/buffers that start small and re-allocate as they grow. Pre-size when the size is known.
7. **Copying where a view would do** - slicing/substring/copying data that could be referenced (spans, slices, memoryview, subarrays).
8. **Large temporary buffers** - per-request big arrays; pool or stream instead.

## C# / .NET

1. **LINQ in hot loops** - `Where/Select/ToList` allocate iterators + lists per call; a plain `for`/`foreach` with no intermediate collection avoids it.
2. **Boxing** - value type -> `object`/interface (non-generic APIs, struct enumerators via interface). Use generics; avoid `object`.
3. **Closures** - capturing lambdas allocate a display class; hoist captured state or use `static` lambdas.
4. **String building** - concatenation in loops -> `StringBuilder`, `string.Create`, or interpolated string handlers.
5. **`params` arrays** - hidden array per call; provide non-params overloads (or `params ReadOnlySpan<T>` on C# 13+).
6. **Unnecessary `Task` allocation** - a method that usually completes synchronously -> `ValueTask` (await once only).
7. **Defensive copies** - large structs passed by value; use `in`/`ref readonly`, `readonly struct`.
8. **Collections sized wrong** - pre-size `List`/`Dictionary` capacity.
9. **Substrings and slices** - `Substring`/`ToArray` -> `ReadOnlySpan<char>`/`AsSpan()`/`Memory<T>`; rent big buffers from `ArrayPool<T>.Shared`.

```csharp
// allocates: closure + iterator + list, every call
var ids = items.Where(i => i.IsActive).Select(i => i.Id).ToList();

// hot path: no intermediate allocations
var ids = new List<int>(items.Count);
foreach (var i in items) if (i.IsActive) ids.Add(i.Id);
```

## TypeScript / JavaScript

1. **`filter().map().reduce()` chains** on large arrays in hot paths - each step allocates a new array; fuse into one `for` loop.
2. **Spread in loops** - `acc = { ...acc, [k]: v }` or `[...arr, x]` inside a loop is O(n^2) allocation; mutate a local object/array, then return it.
3. **Deep clone via `JSON.parse(JSON.stringify(x))`** - allocates a full string and graph; avoid cloning, or use `structuredClone` when a clone is genuinely needed.
4. **Closures/handlers created per call or per render** in hot code - hoist to module scope.
5. **Regex literals and `new RegExp` in loops** with dynamic patterns - build once.
6. **Buffers** - `Buffer.concat` in a loop; collect chunks and concat once, or stream.

## Python

1. **Materialized lists** - `[f(x) for x in xs]` passed straight to `sum`/`any`/`max` -> generator expression.
2. **String concatenation in loops** - `s += part` -> `"".join(parts)`; bytes -> `bytearray` or `io.BytesIO`.
3. **Many small objects** - classes instantiated millions of times -> `__slots__` (or `@dataclass(slots=True)`), or tuples/NamedTuple.
4. **Copies** - `copy.deepcopy`, `list(x)` / slicing large lists, pandas operations that copy; use views (`memoryview`, NumPy views) and in-place ops where safe.
5. **Per-call setup** - `re.compile` inside a function called in a loop (module-level constant instead), building lookup dicts per call.
6. **Reading whole files** - `f.read()` on large files -> iterate lines or chunks.

## Java / Kotlin

1. **Autoboxing** - `List<Integer>`, `Stream<Integer>`, `Map<Long, ...>` in hot loops; use `IntStream`/primitive arrays or a primitive-collections library. Kotlin: `IntArray` vs `Array<Int>`, nullable `Int?` boxes.
2. **Streams in tight loops** - per-call stream pipeline objects and lambdas; a plain loop is cheaper on the hottest paths.
3. **String concatenation in loops** -> `StringBuilder` (single-expression `+` is fine).
4. **Recreated heavy objects** - `Pattern.compile` per call (also hidden in `String.split`/`replaceAll` with regex), new `ObjectMapper` per request; make them static/shared (both are thread-safe once configured).
5. **Varargs** - hidden array per call (e.g. logging with many params; use parameterized logging and level checks).
6. **Unsized collections** - pass initial capacity to `ArrayList`/`HashMap` when known.
7. **Kotlin** - sequences (`asSequence()`) avoid intermediate lists for long chains; `inline` higher-order functions avoid lambda objects.

## Go

1. **Heap escapes** - read `go build -gcflags=-m` output: returning pointers to locals, values stored in interfaces, closures capturing variables, slices/maps of unknown size. Return values instead of pointers where small; keep data on the stack.
2. **Interface boxing** - passing concrete values as `any`/interfaces in hot code (including `fmt` and loggers); use typed APIs.
3. **`fmt.Sprintf` for simple conversions** -> `strconv.Itoa`/`strconv.AppendInt`, `strings.Builder`.
4. **`[]byte` <-> `string` conversions** in loops copy each time; stay in one representation (`bytes` package functions, `strings.Builder`).
5. **`append` growth** - `make([]T, 0, n)` when n is known; same for `make(map[K]V, n)`.
6. **Short-lived buffers** - `sync.Pool` for reusable buffers on hot paths (reset before reuse; don't pool huge or rarely used objects).

## Output format

```
## Findings (hot paths only)
<stack detected; which paths were treated as hot and why>
- <file:line> [closure] ... -> ...
- <file:line> [boxing] ... -> ...

## Rewritten
<code>

## Verify
<stack-specific benchmark/profile to compare allocations before and after,
 e.g. BenchmarkDotNet [MemoryDiagnoser] Allocated/Gen0, go test -benchmem allocs/op, JMH -prof gc>
```

## Principles

- **Only optimize hot paths.** Readable transforms (LINQ, streams, comprehensions, array methods) win everywhere else - don't uglify cold code to save nanoseconds.
- **Measure before and after** (`benchmark-setup`) - guesses mislead, and JITs/compilers already remove some allocations.
- Allocation reduction is about *steady-state GC pressure*, not one-off startup cost.
- Keep behavior identical; call out any semantic change (e.g. lazy vs eager evaluation).

## How to use it & best prompts

"Why does this allocate so much", "reduce GC pressure in this handler", "find boxing/closures here", "why does this escape to the heap", "cut allocations in this hot loop". Pairs with `benchmark-setup`, `gc-pressure-auditor`, and `hotpath-profiler-assistant`.
