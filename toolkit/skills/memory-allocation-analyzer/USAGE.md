# How to use - Memory Allocation Analyzer

**What it is.** Finds avoidable heap allocations on hot paths and rewrites them to cut GC pressure, in any stack: LINQ, boxing, closures, `params`, `Task` allocations in .NET; array-method chains, spreads in loops, JSON clones in JS/TS; materialized lists, string concatenation, missing `__slots__` in Python; autoboxing, streams, regex recompilation in Java/Kotlin; heap escapes, interface boxing, `fmt.Sprintf`, append growth in Go.

**When to reach for it.** A hot path allocates heavily, GC pressure is high, or you're tuning a throughput-critical function.

**How to use it.** Point it at the code (path or GitHub URL), ideally with the profile that shows it's hot. Example prompts:
- "Why does this ASP.NET Core handler allocate so much?"
- "Reduce allocations in this Node request parser."
- "Why do these values escape to the heap in my Go service?"
- "Cut boxing in this Java pricing loop."
- "This Python ETL step uses too much memory - find the waste."

**Get the best out of it.** Tell it which paths are actually hot - it deliberately leaves cold code readable. Verify before/after with `benchmark-setup` (BenchmarkDotNet `[MemoryDiagnoser]`, JMH `-prof gc`, `go test -benchmem`, tracemalloc/memray).

**What you get.** Findings tagged by type with file:line, the rewritten code, and the exact benchmark or profile to confirm the improvement on your stack.

**Won't do.** It won't uglify cold code to save nanoseconds, and it won't guess at hotness - confirm with a profiler (`hotpath-profiler-assistant`).
