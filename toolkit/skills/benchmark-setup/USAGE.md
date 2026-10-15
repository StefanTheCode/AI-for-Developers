# How to use - Benchmark Setup

**What it is.** Writes correct micro-benchmarks with the right tool for your stack and interprets the results - time, ratio vs baseline, allocations, and noise. Tools: **BenchmarkDotNet** (.NET), **JMH** (Java/Kotlin), **pytest-benchmark / pyperf** (Python), **Vitest bench / tinybench / mitata** (JS/TS), **testing.B + benchstat** (Go), **criterion** (Rust).

**When to reach for it.** Comparing implementations, measuring a function, checking whether an optimization is real, or making sense of benchmark output.

**How to use it.** Point it at the code (path or GitHub URL), or paste the functions/results. Example prompts:
- "Benchmark these two functions."
- "Set up BenchmarkDotNet to compare these two methods."
- "Is `Array.join` faster than string concat here? Set up a Vitest bench."
- "Write a Go benchmark for this parser and compare with benchstat."
- "Interpret these JMH results for me."

**Get the best out of it.** Give it realistic input sizes. Keep allocation measurement on - it's often the real story. Confirm the path is actually hot (profile first) before optimizing.

**Won't do.** It won't profile a whole app - it's for focused micro-benchmarks. It won't benchmark Debug/dev builds, and it won't declare a winner when the difference is inside the noise.
