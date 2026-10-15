# Performance Lab - Demo Script
## YouTube / LinkedIn Video Guide

> **Note:** This script demos the C# implementation. The same MCP server can be built with the official MCP SDKs for TypeScript, Python, Java or Go - worth a one-line mention on camera for non-.NET viewers.

---

## OPENING HOOK (0:00 - 0:30)

**What to say:**
> "What if you could ask your AI assistant: 'Compare these two .NET endpoints - which one has ThreadPool starvation?' and it would actually run load tests, analyze the results, and tell you exactly what to fix? That's what we're building today."

**What to show:** Final result - Copilot chat showing a comparison result with the verdict and fix.

---

## SETUP (0:30 - 1:30)

**What to say:**
> "We have three services running. A sample .NET API on port 5100 - it's got intentionally broken endpoints so we can see all the classic problems. An MCP server on 5200 - this is what the AI talks to. And a Blazor dashboard on 5300 to visualize the results."

**Screen actions:**
1. Show the three terminal windows side by side
2. Open http://localhost:5300 (dashboard) - show it's empty
3. Open http://localhost:5100/endpoints - show the available endpoints

---

## DEMO 1: BASELINE TEST (1:30 - 3:00)

**Copilot prompt:**
```
Run a load test against http://localhost:5100/fast for 10 seconds with 20 concurrent users
```

**What to say while it runs:**
> "The MCP server is firing 20 concurrent HTTP connections and tracking every response. When it finishes it calculates RPS, average latency, and the p50/p95/p99 percentiles."

**What to show:** The tool output appearing in Copilot chat. Point out:
- RPS (~2000+)
- Average latency (<5ms)
- p99 close to p50 (healthy)

**Copilot prompt:**
```
List all results
```

---

## DEMO 2: THE THREAD.SLEEP TRAP (3:00 - 5:30)

**What to say:**
> "Now let's test the broken one. This endpoint calls Thread.Sleep - it looks harmless, but under load it destroys throughput because it occupies a ThreadPool thread while it sleeps."

**Copilot prompt:**
```
Run a load test against http://localhost:5100/slow-thread-sleep for 15 seconds with 20 concurrent users
```

**While it runs:**
> "Watch what happens. 20 users, 500ms sleep each. After the first 40 threads are occupied, new requests start queuing. The p99 is going to be enormous."

**After results appear, point out:**
- Low RPS (2-3 vs 2000+)
- p99 is 10-50× higher than p50 (starvation signature)
- Severity: Critical or High

**Copilot prompt:**
```
Detect threadpool starvation for result <ID>
```

**What to say:**
> "The tool is checking the p99/p50 ratio. A healthy API has a ratio of 2-3. Starvation shows ratios of 20-100."

---

## DEMO 3: THE FIX (5:30 - 7:30)

**What to say:**
> "Here's the fix: just change Thread.Sleep to await Task.Delay. Same behavior from the caller's perspective - but it releases the thread while waiting instead of blocking it."

**Show the code diff:**
```csharp
// ❌ Before
Thread.Sleep(ms);

// ✅ After
await Task.Delay(ms);
```

**Copilot prompt:**
```
Compare http://localhost:5100/slow-thread-sleep with http://localhost:5100/slow-task-delay using 20 concurrent users for 15 seconds
```

**After results:**
> "There it is. 800% improvement in throughput. Same behavior, same latency per request - but now the thread is free to handle other requests while it waits."

**Show the dashboard:** Navigate to http://localhost:5300, show both results side by side, switch to the Compare page.

---

## DEMO 4: N+1 VS TASK.WHENALL (7:30 - 9:30)

**What to say:**
> "The next classic problem: sequential database queries. Every endpoint does 5 queries - but the first version runs them one after another, the second runs them in parallel."

**Copilot prompt:**
```
Compare http://localhost:5100/database-simulation with http://localhost:5100/optimized-version
```

**After results:**
> "5× faster. The code is identical except Task.WhenAll instead of a for loop. This is the most common performance win I see in .NET codebases - if your queries don't depend on each other, don't run them sequentially."

**Show the code:**
```csharp
// ❌ Sequential - ~250ms
for (int i = 1; i <= 5; i++)
    await Task.Delay(50);

// ✅ Parallel - ~50ms
var tasks = Enumerable.Range(1, 5).Select(async i => {
    await Task.Delay(50);
    return $"Record {i}";
});
await Task.WhenAll(tasks);
```

---

## DEMO 5: GENERATE A REPORT (9:30 - 11:00)

**Copilot prompt:**
```
Generate a full performance report for all results from the database comparison
```

**What to say:**
> "This gives us a structured markdown report with the full metric breakdown, diagnosis, and recommendations - ready to paste into a PR description, a Notion doc, or a Confluence page."

---

## DEMO 6: ASK FOR OPTIMIZATION ADVICE (11:00 - 12:30)

**Copilot prompt:**
```
What .NET optimizations would you suggest for threadpool starvation issues?
```

**What to say:**
> "The tool returns pattern-based advice baked in from .NET performance engineering knowledge. Not hallucinated - these are the actual fixes, with code examples."

---

## CLOSING (12:30 - 13:30)

**What to say:**
> "The important pattern here is not the project itself - it's the idea. You can expose any domain-specific tool to an AI client via MCP. The AI provides the reasoning layer; your tools provide the grounded, real-world data.
>
> Performance diagnosis is a great use case because the AI can't guess what's slow in your specific API - it needs real numbers. MCP gives it real numbers.
>
> The full project is in the description. Grab it, swap out the sample API for your own endpoints, and see what it finds."

---

## KEY TALKING POINTS

- **MCP is just HTTP + a schema.** The complexity is in the tools you expose.
- **[Fact] vs [Fiction]:** The AI doesn't guess what's slow - it runs real tests.
- **p50/p99 ratio** is the ThreadPool starvation signature. If p99 is 10-20× p50, you have blocking calls.
- **Thread.Sleep vs Task.Delay** - same delay, completely different thread semantics under load.
- **Task.WhenAll** - if N queries don't depend on each other, run them in parallel. This alone can be a 5× win.
- **Rule-based + AI reasoning** - the analyzer detects the pattern, the AI provides the context and fix.

---

## BACKUP PROMPTS (if demo goes wrong)

```
List all results
Run a load test against http://localhost:5100/health for 5 seconds with 5 users
Analyze the results for <any ID>
Suggest optimizations for general .NET performance
```
