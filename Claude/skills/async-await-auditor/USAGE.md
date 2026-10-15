# How to use - Async / Concurrency Auditor

**What it is.** A skill that scans async and concurrent code for correctness and performance bugs, flags each one by severity (🔴 bug / 🟡 smell / 🟢 nit), and rewrites the code. It detects your stack first, then applies a universal checklist (blocking in async, lost errors, missing cancellation, accidental sequential work, unbounded concurrency, leaks, shared state) plus stack-specific checks:
- **C#/.NET** - `.Result`/`.Wait()`, `async void`, fire-and-forget, `CancellationToken`, `ConfigureAwait`, `Task.Run` misuse, `ValueTask`.
- **JavaScript/TypeScript** - floating promises, `forEach(async ...)`, `await` in loops vs `Promise.all`, blocking the event loop, `AbortController`.
- **Python** - blocking calls in `asyncio`, forgotten `await`, dropped `create_task`, `gather` vs `TaskGroup`, cancellation.
- **Java/Kotlin** - `CompletableFuture.join()/get()` blocking, common-pool misuse, virtual thread pinning, coroutine scope leaks.
- **Go** - goroutine leaks, `context.Context` propagation, `WaitGroup`/`errgroup`, races.
- **Rust** - blocking in async, holding locks across `.await`, detached tasks.

**When to reach for it.** A hang or deadlock, a frozen event loop, thread-pool starvation under load, errors that vanish, a code review of async-heavy code, or before shipping a new background worker / handler.

**How to use it.** Point it at a **file**, **folder**, **project/repo**, or **GitHub URL** (it clones and scans). Example prompts:
- "Review this async code for deadlocks."
- "Review this asyncio code - the service freezes under load."
- "Find floating promises in `src/`."
- "Check this Go service for goroutine leaks."
- "My ASP.NET Core API deadlocks under load - check the Services folder."

**How to get the best out of it.**
- **Include the caller, not just the function.** A blocking call three levels up is often the real bug.
- **Say where it runs** (web request, worker, library, UI, CLI). Deadlock and blocking risk depend on it.
- **Describe the symptom** if you have one (hangs, slow under load, errors vanishing).
- Ask it to **explain the mechanism** (why it deadlocks, why the task got collected) if you want to understand, not just patch.

**What it won't do.** It reasons from code, not a running profiler - for starvation or leaks it tells you what to measure (`go test -race`, JFR pinning events, asyncio debug mode). It won't parallelize work on a non-thread-safe resource like a shared DB context or session; it actively warns against that.
