---
name: async-await-auditor
description: Audit async and concurrent code in any language for correctness and performance bugs - C#/.NET (async/await, Task), JavaScript/TypeScript (Promises, async/await), Python (asyncio), Java/Kotlin (CompletableFuture, virtual threads, coroutines), Go (goroutines, channels, context), Rust (tokio/async). Use this skill whenever the user shares async or concurrent code, or asks about deadlocks, hangs, blocking calls, .Result/.Wait(), async void, ConfigureAwait, floating or unhandled promises, await in a loop vs Promise.all, blocking the event loop, forgotten await, asyncio.gather/TaskGroup, CompletableFuture.join, virtual thread pinning, goroutine leaks, context cancellation, WaitGroup/errgroup, holding a lock across .await, fire-and-forget tasks, cancellation tokens, or "why does my async code hang". Always use this skill for async/concurrency reviews instead of answering from memory; it applies a fixed checklist and rewrites the code.
category: Performance
---

# Async / Concurrency Auditor

Find async and concurrency bugs (deadlocks, blocked threads or event loops, lost errors, leaks, missing cancellation), then return corrected code with a one-line reason for each fix.

## Input - point it at your code

Work directly from whatever target the user gives: a **file**, a **folder**, a whole **project/repo**, or a **GitHub URL** (clone first: `git clone --depth 1 <url> /tmp/repo`). Find the async code yourself with Glob/Grep/Bash. Always report which files you scanned; if it's clean, say so plainly instead of inventing issues. Group findings by file.

## Step 1 - detect the stack and runtime

| Manifest | Stack | What "async" means there |
|---|---|---|
| `*.csproj` / `*.sln` | C# / .NET | `Task`, `async`/`await`, thread pool, sync context |
| `package.json` | JS / TS (Node, Bun, Deno, browser) | Promises on a single-threaded event loop |
| `pyproject.toml` / `requirements*.txt` | Python | `asyncio` event loop (or trio/anyio), plus threads |
| `pom.xml` / `build.gradle(.kts)` | Java / Kotlin | `CompletableFuture`, executors, virtual threads, coroutines, Reactor |
| `go.mod` | Go | goroutines, channels, `context.Context` |
| `Cargo.toml` | Rust | `async fn` on an executor (usually tokio) |

Also establish **where the code runs** (web request, background worker, library, UI, CLI). Deadlock and blocking risk depend on it. Ask only if you can't tell from the code.

## Workflow

1. **Read the call chain.** Async correctness is about the whole chain, not one function. Find entry points and how far async flows.
2. **Run the universal checklist**, then the stack-specific section.
3. **Rate each finding:** 🔴 bug / 🟡 smell / 🟢 nit.
4. **Rewrite** preserving behavior; call out any behavior change.
5. **Explain each fix** in one line.

## Universal checklist (every language)

1. **Blocking inside async code** - sync I/O, sleeps, CPU-heavy loops, or blocking on another async result. Starves the thread pool or freezes the event loop.
2. **Lost errors** - fire-and-forget work whose failure nobody observes.
3. **Missing cancellation / timeouts** - work keeps running after the caller gave up; no deadline on network calls.
4. **Accidental sequential work** - independent awaits run one after another.
5. **Unbounded concurrency** - starting N tasks for N items with no limit (connection pool exhaustion, rate limits, memory).
6. **Leaks** - tasks/goroutines/listeners that never finish or are never awaited/joined.
7. **Shared state** - non-thread-safe objects (DB sessions/contexts, clients) used concurrently; locks held across suspension points.

## C# / .NET

Sweep: `grep -rn "\.Result\b\|\.Wait()\|GetAwaiter().GetResult()\|async void\|Task.Run\|Thread.Sleep" --include=*.cs <target> | grep -v "/obj/\|/bin/"`

1. 🔴 **Sync-over-async** (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) - deadlocks under a sync context, starves the thread pool otherwise. Fix: async all the way and `await`. At a sync boundary you truly can't change, isolate it and document why.
2. 🔴 **`async void`** - exceptions crash the process and can't be awaited. Fix: return `Task`. Event handlers are the only exception; keep them thin with try/catch.
3. 🔴 **Fire-and-forget** (`_ = DoAsync();`) in request scope - exceptions vanish and work dies with the request. Fix: await, or hand off to `BackgroundService` / `Channel<T>` / a queue.
4. 🟡 **Missing `CancellationToken`** - accept it and pass it to the deepest call (EF, `HttpClient`, etc.). ASP.NET Core binds the action's `CancellationToken` to request abort.
5. 🟡 **`ConfigureAwait(false)`** - use it in library code; don't litter it through ASP.NET Core app code (no sync context there).
6. 🟡 **Await elision** - `return SomeAsync();` is fine for a one-liner, but NOT inside `using`/`try`, where it changes when disposal and exception handling happen.
7. 🟡 **Sequential independent awaits** - start them, then `await Task.WhenAll(...)`. A single `DbContext` is NOT thread-safe: never parallelize queries on one context.
8. 🟡 **`Task.Run` to "make it async"** - wastes a pool thread on the server. Call the async API directly; reserve `Task.Run` for CPU-bound work off a UI thread.
9. 🟡 **Async lambda where `Action` is expected** (`list.ForEach(async x => ...)`) - silently becomes `async void`. Use a loop with `await` or `Task.WhenAll(list.Select(async x => ...))`; bound concurrency with `Parallel.ForEachAsync` or `SemaphoreSlim`.
10. 🟡 **Blocking inside async** - `Thread.Sleep` -> `await Task.Delay`; `lock` around an await is a compile error, so use `SemaphoreSlim.WaitAsync()`.
11. 🟢 **`ValueTask` misuse** - await it exactly once, immediately; never store or double-await. Use only on proven hot paths that often complete synchronously.

## JavaScript / TypeScript

Sweep: `grep -rnE "forEach\(async|\.then\(|new Promise\(|readFileSync|execSync" --include=*.{ts,js} <target> | grep -v node_modules`. Recommend `@typescript-eslint/no-floating-promises` and `no-misused-promises` to catch these permanently.

1. 🔴 **Floating promises** - a call to an async function with no `await`, `return`, or `.catch`. Errors become unhandled rejections (Node terminates the process on them by default since v15).
2. 🔴 **`array.forEach(async ...)`** - `forEach` ignores the returned promises; nothing is awaited. Use `for...of` with `await` (sequential) or `await Promise.all(items.map(async ...))` (concurrent).
3. 🟡 **`await` in a loop for independent work** - use `Promise.all`; use `Promise.allSettled` when one failure shouldn't discard the rest. Bound large fan-outs (e.g. `p-limit` or chunking).
4. 🟡 **Blocking the event loop** - `*Sync` fs/crypto/child_process calls, huge `JSON.parse`, tight CPU loops on a request path. Move to async APIs or a `worker_threads` worker.
5. 🟡 **No cancellation / timeout** - pass an `AbortSignal` (`AbortController`, `AbortSignal.timeout(ms)`) to `fetch` and other abortable APIs, and propagate it through your own functions.
6. 🟡 **Promise constructor anti-pattern** - wrapping something that already returns a promise in `new Promise`; a `throw` inside an async executor is lost. Return the promise directly.
7. 🟢 **Mixing `.then()` chains and `await`** in one function - pick one; `return await` inside `try` is needed to catch the rejection there.

## Python (asyncio)

Sweep: `grep -rnE "time\.sleep|requests\.|open\(|create_task|run_until_complete|asyncio\.run" --include=*.py <target>`

1. 🔴 **Blocking calls in `async def`** - `time.sleep`, `requests`, sync DB drivers, heavy CPU. They freeze the whole loop. Use `await asyncio.sleep`, `httpx.AsyncClient`/`aiohttp`, async drivers, or `await asyncio.to_thread(fn, ...)` for unavoidable sync calls.
2. 🔴 **Forgotten `await`** - the coroutine never runs ("coroutine was never awaited" warning). Run tests with warnings as errors to catch it.
3. 🔴 **Dropped `asyncio.create_task(...)` result** - the loop keeps only a weak reference, so the task can be garbage-collected mid-flight and its exception is never seen. Keep a reference, or use `asyncio.TaskGroup` (3.11+).
4. 🟡 **`gather` without error strategy** - by default the first exception propagates but the other tasks keep running. Prefer `TaskGroup` (cancels siblings on failure) or handle `return_exceptions=True` results explicitly.
5. 🟡 **Swallowed cancellation** - `except Exception` is fine, but catching `BaseException`/bare `except:` or not re-raising `asyncio.CancelledError` breaks shutdown and timeouts. Use `asyncio.timeout()` (3.11+) or `asyncio.wait_for` for deadlines.
6. 🟡 **Unbounded fan-out** - `gather(*[fetch(u) for u in urls])` over thousands of URLs. Limit with `asyncio.Semaphore`.
7. 🟡 **Sync ORM sessions in async handlers** (e.g. SQLAlchemy `Session` inside FastAPI `async def`) - use the async session, or make the endpoint a plain `def` so the framework runs it in a threadpool.

## Java / Kotlin

Sweep: `grep -rnE "\.join\(\)|\.get\(\)|supplyAsync|runAsync|synchronized|newVirtualThreadPerTaskExecutor|runBlocking|GlobalScope" <target>/src`

1. 🔴 **Blocking on futures** - `CompletableFuture.join()`/`get()` inside another async stage or on a small pool can deadlock and starves the pool. Compose with `thenCompose`/`thenCombine`/`allOf`, and block only at the outermost edge, with a timeout (`get(timeout, unit)` or `orTimeout`).
2. 🟡 **Default common pool** - `supplyAsync(task)` without an executor runs blocking I/O on `ForkJoinPool.commonPool()`. Pass a dedicated executor for I/O.
3. 🟡 **Lost exceptions** - futures nobody joins; add `exceptionally`/`handle`/`whenComplete` or make sure the result is observed.
4. 🟡 **Virtual thread pinning** - on JDK 21-23, blocking inside `synchronized` (or native frames) pins the carrier thread; replace with `ReentrantLock` on hot paths. JDK 24 (JEP 491) removed most `synchronized` pinning. Diagnose with the `jdk.VirtualThreadPinned` JFR event. Don't pool virtual threads; limit concurrency with a `Semaphore` instead.
5. 🟡 **ThreadLocal assumptions** - heavy `ThreadLocal` caches multiply with millions of virtual threads; MDC/security context must be propagated to async stages explicitly.
6. 🟡 **Kotlin coroutines** - `GlobalScope.launch` leaks work outside structured concurrency; `runBlocking` inside a coroutine or request thread blocks it; blocking calls belong in `withContext(Dispatchers.IO)`; don't swallow `CancellationException`.
7. 🟢 **Reactor/WebFlux** - any blocking call inside a reactive chain (`block()`, JDBC) stalls the event loop; offload with `subscribeOn(Schedulers.boundedElastic())`. BlockHound can detect it in tests.

## Go

Sweep: `grep -rnE "go func|context\.Background\(\)|WaitGroup|time\.Sleep|make\(chan" --include=*.go <target>`. Run tests with `go test -race`; consider `go.uber.org/goleak` for leak checks.

1. 🔴 **Goroutine leaks** - a goroutine blocked forever on a channel send/receive nobody will complete (e.g. a result channel abandoned after a timeout). Use buffered channels for single results, `select` on `ctx.Done()`, and make every goroutine's exit path explicit.
2. 🔴 **Data races** - shared maps/slices written from multiple goroutines without a mutex or channel ownership. `-race` finds them.
3. 🟡 **Ignored `context.Context`** - `context.Background()`/`TODO()` deep in request code, or a `ctx` parameter never passed to DB/HTTP calls. Thread the request context through; set deadlines with `context.WithTimeout` and always `defer cancel()`.
4. 🟡 **`WaitGroup` misuse** - `wg.Add` inside the goroutine (race with `Wait`), missing `defer wg.Done()`, or errors dropped. Prefer `golang.org/x/sync/errgroup` (`errgroup.WithContext`, `g.SetLimit(n)`) for fan-out with errors and cancellation.
5. 🟡 **Unbounded goroutines** - one per item over large inputs. Bound with `errgroup.SetLimit`, a worker pool, or a semaphore channel.
6. 🟢 **Loop variable capture** - only an issue before Go 1.22 (per-iteration loop variables since 1.22); check the `go` directive in `go.mod`.

## Rust (tokio)

1. 🔴 **Blocking in async** - `std::thread::sleep`, `std::fs`, sync HTTP clients, or heavy CPU inside `async fn` blocks an executor worker. Use `tokio::time::sleep`, `tokio::fs`, async clients, or `tokio::task::spawn_blocking`.
2. 🔴 **Holding a `std::sync::Mutex` guard across `.await`** - can deadlock and makes the future `!Send`. Drop the guard before awaiting (scope it), or use `tokio::sync::Mutex` when the lock must span an await.
3. 🟡 **Detached `tokio::spawn`** - a dropped `JoinHandle` hides panics and errors. Keep handles or use `JoinSet`.
4. 🟡 **Cancellation safety** - futures in `tokio::select!` branches can be dropped mid-way; make sure the dropped branch doesn't lose data. Use `tokio::time::timeout` for deadlines.

## Output format

```
## Summary
<stack + runtime detected; headline issues and the deadlock / starvation / leak risk, if any>

## Findings
🔴 [Sync-over-async] <file:line> - <why it's a bug> -> <fix>
🟡 [Missing cancellation] ... -> ...

## Corrected code
<rewritten, ready to paste>

## Notes
<behavior changes, anything the caller must also update>
```

## Principles

- Async is a chain - a single blocking link can stall or deadlock the whole thing.
- Every task, promise, or goroutine needs an owner who observes its result and its failure.
- Propagate cancellation and deadlines end to end.
- Concurrency must be bounded; "start everything at once" is not a strategy.
- Never share a non-thread-safe resource (DB context/session, connection) across concurrent work.
- Prefer making the chain async over hiding a blocking call.
