# 06 · Background Job Runner — Build Guide

> **Level:** Intermediate · **Time:** 4–7 hours · **You'll need:** .NET 10 SDK, an AI assistant
>
> Part of the **[October Build Challenge](../../CHALLENGE.md)**. The [README](README.md) explains the finished project. This guide is for building it yourself with AI.

---

## The goal

A producer hosted service puts jobs into a bounded `Channel<Job>`. A consumer hosted service processes them with capped retries, exponential backoff, and a dead-letter for jobs that never succeed. The app shuts down cleanly.

**The real goal:** understand what happens to work when things fail, and when the app stops.

---

## Step-by-step

### Step 1 — Generic Host

```bash
dotnet new console -n JobRunner -o src/JobRunner
dotnet add src/JobRunner package Microsoft.Extensions.Hosting
```

`Host.CreateApplicationBuilder(args)` gives you DI, logging, config, and lifetime.

### Step 2 — Job + queue

> **Prompt:** "Create a `Job` record and a `JobQueue` around a BOUNDED `Channel<Job>` (capacity 100, FullMode = Wait). Expose `EnqueueAsync`, `DequeueAllAsync`, and `Complete`. Explain back-pressure and what `SingleReader` / `SingleWriter` promise."

**Check yourself:** The consumer is 10× slower than the producer. What happens with a bounded channel? With an unbounded one?

### Step 3 — Producer

A `BackgroundService` that enqueues 10 jobs and then calls `Complete()`.

### Step 4 — Consumer with retries

> **Prompt:** "Create a `JobProcessor : BackgroundService` that reads with `await foreach`. Each job gets max 3 attempts with exponential backoff (200ms, 400ms). After the last failure, log it as dead-lettered. Cancellation during shutdown must NOT count as a failure. Show the plan first."

### Step 5 — Clean exit

When the channel drains, call `IHostApplicationLifetime.StopApplication()`.

### Step 6 — Wire it up

Register `JobQueue` as a **singleton**, plus both hosted services.

**Check yourself:** Why singleton? What breaks if it's transient?

---

## ⚠️ Where AI usually gets it wrong here

| AI tends to... | Why it's wrong | What to do instead |
|---|---|---|
| `ConcurrentQueue` + `while(true)` + `Thread.Sleep` | Burns CPU, blocks threads, no back-pressure | `Channel<T>` |
| Unbounded channel | A slow consumer means unlimited memory | Bounded + `Wait` |
| Retry forever / no delay | Hammers a service that's already down | Cap + exponential backoff + jitter |
| Inject a scoped `DbContext` into the hosted service | Captive dependency: one context lives forever | `IServiceScopeFactory`, one scope per job |
| Let exceptions escape `ExecuteAsync` | Since .NET 6 that **stops the whole host** | try/catch per job |
| Treat `OperationCanceledException` as a normal failure | You "retry" during shutdown | Rethrow only when shutdown was requested |

### 🔍 Review moment: our own code

`JobProcessor` does:

```csharp
catch (OperationCanceledException) { throw; }
```

But an `HttpClient` timeout is *also* an `OperationCanceledException` (`TaskCanceledException`), and it has nothing to do with shutdown. In a real job, a single timeout would bypass the retry and **kill the worker**. Fix it:

```csharp
catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
```

Then ask yourself: why is the `when` filter the right tool here?

---

## 🔨 Break it on purpose

1. Press **Ctrl+C** while jobs are running. What happens to the job in progress? To the jobs still in the channel?
2. Set the failure rate to 100%. Does every job dead-letter after exactly 3 attempts?
3. Set capacity to 1 and add `Task.Delay(2000)` in the worker. Watch the producer wait.
4. Throw an unhandled exception outside the retry loop. What does the host do?

---

## ✅ Definition of done

- [ ] Jobs are processed, retried, and dead-lettered as expected
- [ ] Ctrl+C shuts down cleanly with no "retrying" logs
- [ ] The cancellation `when` filter is fixed
- [ ] You can explain back-pressure in one sentence
- [ ] You can explain why jobs are lost on a crash, and what a real broker adds
- [ ] You wrote down **one thing the AI got wrong**

## 🚀 Stretch goal

Add a Minimal API `POST /jobs` that enqueues jobs, and replace the hand-rolled retry with a `Microsoft.Extensions.Resilience` / Polly pipeline (backoff **with jitter** + circuit breaker).

---

**Doing the [October Build Challenge](../../CHALLENGE.md)?** Share your repo and the one thing AI got wrong in **[AI for Developers](https://www.skool.com/ai-for-dotnet-developers/about)**. That's also where the Advanced & Expert levels come with walkthroughs, roadmaps, and someone to ask when you get stuck.
