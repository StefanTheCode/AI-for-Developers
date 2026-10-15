---
title: "I Built an MCP Server That Tests .NET API Performance (And It's Actually Useful)"
subtitle: "What if your AI coding assistant could run load tests, detect ThreadPool starvation, and suggest fixes - all from a chat message?"
date: "May 14 2026"
category: "AI"
image: "/images/mcp-performance-lab.png"
---

> **Not on .NET?** This post walks through the C# implementation, but the same MCP server can be built with the official MCP SDKs for TypeScript, Python, Java or Go. The tools and load-testing logic translate directly; the diagnosis rules map to each runtime's own version of the same problems.

I want to tell you about the most practical AI tool I've built for my own .NET workflow.

Not a chatbot. Not a code generator. A **performance lab** where I describe what I want to test in plain English, and an AI agent runs the load tests, analyzes the results, detects problems, and proposes fixes - all through a conversation.

Here's what a real session looks like:

> **Me:** Compare http://localhost:5100/slow-thread-sleep with http://localhost:5100/slow-task-delay - 15 seconds, 20 concurrent users.

> **Copilot:**
> ```
> BASELINE  → /slow-thread-sleep    CANDIDATE → /slow-task-delay
>
> Throughput:  35.1 RPS              →  39.9 RPS   (✅ +13.5%)
> Average:     554ms                 →  501ms       (✅ -9.6%)
> P95:         1000ms                →  503ms       (✅ -49.7%)
> P99:         1002ms                →  507ms       (✅ -49.3%)
>
> VERDICT: ✅ Candidate is faster
> ```

One message. The AI ran two real load tests against a live server and gave me a side-by-side comparison with percentage deltas.

The p95/p99 improvement is the telling number. `Thread.Sleep` saturates the ThreadPool - requests queue up waiting for a thread, and tail latency explodes. `Task.Delay` releases the thread while waiting, so 20 concurrent users are handled efficiently. Same delay, completely different behaviour under load.

---

## What Is MCP?

MCP (Model Context Protocol) is an open standard for exposing tools to AI clients. Instead of the AI guessing what your system does, you give it explicit, callable functions - like "run a load test" or "analyze these results."

The AI becomes the **orchestrator**: it decides which tool to call, what parameters to pass, and how to reason about the output. You write the tools; the AI decides when and how to use them.

This is different from function calling in OpenAI or tool use in Anthropic. MCP is transport-agnostic (HTTP or stdio) and any MCP-compatible client - GitHub Copilot, Claude Desktop, Cursor - can connect to the same server without changes.

---

## Project Structure

Four projects, each with a clear responsibility:

```
PerformanceLab.Api        → port 5100  - sample ASP.NET Core API with broken endpoints
PerformanceLab.McpServer  → port 5200  - MCP server + load test engine
PerformanceLab.Dashboard  → port 5300  - Blazor dashboard for visualizing results
PerformanceLab.Shared                  - shared models
```

![Project structure in VS Code showing the four projects](_screenshots/project-structure.png)

---

## Part 1: The Sample API - Intentionally Broken

The first project is a minimal ASP.NET Core API where every endpoint demonstrates a specific performance anti-pattern. The goal is to have real targets we can measure.

### Thread.Sleep vs Task.Delay

```csharp
// ❌ Villain: blocks a ThreadPool thread for the entire wait
app.MapGet("/slow-thread-sleep", (int ms = 500) =>
{
    Thread.Sleep(ms);
    return Results.Ok(new { message = "done", blockedMs = ms });
});

// ✅ Hero: releases the thread back to the pool while waiting
app.MapGet("/slow-task-delay", async (int ms = 500) =>
{
    await Task.Delay(ms);
    return Results.Ok(new { message = "done", waitedMs = ms });
});
```

From the caller's perspective these are identical. Under load with 20 concurrent users, they're worlds apart. `Thread.Sleep` holds onto a ThreadPool thread for 500ms. The .NET ThreadPool starts with a limited number of threads. Once they're all sleeping, new requests queue up. p99 climbs to 10× the p50 - the classic starvation signature.

### N+1 vs Task.WhenAll

```csharp
// ❌ Sequential - 5 queries one after another (~250ms total)
app.MapGet("/database-simulation", async () =>
{
    var results = new List<string>();
    for (int i = 1; i <= 5; i++)
    {
        await Task.Delay(50); // simulate a DB query
        results.Add($"Record {i}");
    }
    return Results.Ok(results);
});

// ✅ Parallel - all 5 queries at the same time (~50ms total)
app.MapGet("/optimized-version", async () =>
{
    var tasks = Enumerable.Range(1, 5).Select(async i =>
    {
        await Task.Delay(50);
        return $"Record {i}";
    });
    var results = await Task.WhenAll(tasks);
    return Results.Ok(results);
});
```

The sequential version runs queries one at a time. If the queries don't depend on each other's results, there's no reason to wait. `Task.WhenAll` runs all five in parallel - a 5× latency reduction with one change.

There are also `/memory-heavy` (allocates 1MB of arrays per request to trigger GC pressure) and `/cpu-heavy` (SHA256 in a loop) endpoints. Each has a distinct performance signature the analyzer can detect.

---

## Part 2: Building the MCP Server

The MCP server is a regular ASP.NET Core app. One NuGet package is all you need:

```bash
dotnet add package ModelContextProtocol.AspNetCore --version 0.3.0-preview.2
```

### Program.cs

```csharp
// Services
builder.Services.AddSingleton<IResultStore, InMemoryResultStore>();
builder.Services.AddScoped<LoadTestRunner>();
builder.Services.AddScoped<ResultAnalyzer>();

// MCP Server - discovers all [McpServerTool] methods automatically
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

// CORS for the Blazor dashboard
builder.Services.AddCors(opts =>
    opts.AddPolicy("dashboard", policy =>
        policy.WithOrigins("http://localhost:5300")
              .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors("dashboard");

// Two surfaces on the same server:
app.MapMcp("/mcp");                                               // ← AI clients connect here
app.MapGet("/api/results", (IResultStore store) =>               // ← Dashboard reads here
    Results.Ok(store.GetAll()));
```

The AI talks to `/mcp`. The Blazor dashboard reads from `/api/results`. They share the same `IResultStore` but are otherwise completely decoupled.

---

## Part 3: Defining the Tools

Tools are C# methods with `[McpServerTool]` and `[Description]` attributes. The description is what the AI reads when deciding whether to call the tool.

```csharp
[McpServerToolType]
public sealed class PerformanceTools(
    LoadTestRunner runner,
    ResultAnalyzer analyzer,
    IResultStore   store,
    ILogger<PerformanceTools> logger)
{
    [McpServerTool(Name = "run_load_test")]
    [Description(
        "Run a load test against an API endpoint. Fires concurrent HTTP requests for the " +
        "specified duration and returns throughput, latency percentiles, and error rate. " +
        "Returns a result ID that can be passed to analyze_results or generate_report.")]
    public async Task<string> RunLoadTest(
        [Description("Full URL of the endpoint to test. Example: http://localhost:5100/fast")]
        string url,

        [Description("How many seconds the test should run. Default: 10")]
        int durationSeconds = 10,

        [Description("Number of virtual concurrent users (parallel HTTP connections). Default: 10")]
        int concurrentUsers = 10,

        CancellationToken cancellationToken = default)
    {
        var request = new LoadTestRequest
        {
            Url             = url,
            DurationSeconds = durationSeconds,
            ConcurrentUsers = concurrentUsers,
        };

        var result   = await runner.RunAsync(request, cancellationToken);
        var analysis = analyzer.Analyze(result);
        store.Add(result, analysis);

        return FormatLoadTestResult(result, analysis);
    }
}
```

A few things worth noting:

- **Descriptions are everything.** The AI decides which tool to call based entirely on the description. Be explicit about what the tool does and what it returns.
- **Tools return strings, not objects.** The AI reads plain text and reasons about it. You don't need a typed schema - just clear, consistent output.
- **`WithToolsFromAssembly()`** scans for all `[McpServerToolType]` classes at startup. You never register tools manually.

The server exposes 10 tools in total: `run_load_test`, `compare_endpoints`, `analyze_results`, `detect_slow_responses`, `detect_threadpool_starvation`, `detect_memory_pressure`, `list_results`, `compare_before_after`, `suggest_optimizations`, and `generate_report`.

![GitHub Copilot chat showing available MCP tools from the performance-lab server](_screenshots/mcp-tools-in-copilot.png)

---

## Part 4: The Load Test Runner

No k6, no JMeter, no external tools. Pure .NET:

```csharp
public async Task<LoadTestResult> RunAsync(LoadTestRequest request, CancellationToken ct)
{
    var records = new ConcurrentBag<RequestRecord>();
    var endAt   = DateTime.UtcNow.AddSeconds(request.DurationSeconds);

    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    linkedCts.CancelAfter(TimeSpan.FromSeconds(request.DurationSeconds + 15));

    var workerTasks = Enumerable
        .Range(0, request.ConcurrentUsers)
        .Select(_ => RunWorkerAsync(request, records, endAt, linkedCts.Token))
        .ToArray();

    await Task.WhenAll(workerTasks);

    return BuildResult(request, [.. records]);
}
```

`Task.WhenAll` spins up N concurrent workers - one per virtual user. Each worker fires requests in a tight loop until the time window closes. The `ConcurrentBag<RequestRecord>` collects every result without locking. Once all workers finish, `BuildResult` sorts the latencies and computes RPS, average, and percentiles.

The `HttpClient` is configured once with `IHttpClientFactory`:

```csharp
builder.Services.AddHttpClient("loadtest", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect       = false,
    MaxConnectionsPerServer = 1000,
});
```

`MaxConnectionsPerServer = 1000` is important. The default is 2 (!) in older .NET versions. Without this, your load test is bottlenecked by connection limits, not the server.

---

## Part 5: The Rule-Based Analyzer

The analyzer is pure C# - no AI, no ML. It applies rules derived from real performance engineering patterns and returns a severity rating with concrete recommendations.

```csharp
public AnalysisResult Analyze(LoadTestResult result)
{
    var issues          = new List<string>();
    var recommendations = new List<string>();
    var optimizations   = new List<string>();

    // ThreadPool starvation: p99 is 10× higher than p50
    // Healthy APIs: p99/p50 ratio of 2-5
    // Starved APIs: p99/p50 ratio of 20-100
    if (result.P50Ms > 0 && result.P99Ms / result.P50Ms > 10)
    {
        var ratio = result.P99Ms / result.P50Ms;
        issues.Add(
            $"High latency variance - p99 is {ratio:F0}× higher than p50. " +
            "Classic threadpool starvation signature.");
        recommendations.Add(
            "Search your codebase for Thread.Sleep(), .Result, .Wait(), " +
            "GetAwaiter().GetResult(). Replace them all with async/await.");
        optimizations.Add("Replace Thread.Sleep with await Task.Delay.");
        optimizations.Add("Replace .Result / .Wait() with await.");
    }

    // GC pause: a single request was far slower than the p99
    if (result.MaxMs > result.P99Ms * 3)
    {
        issues.Add(
            $"Max ({result.MaxMs:F0}ms) is 3× higher than p99 ({result.P99Ms:F0}ms). " +
            "Suggests occasional GC pauses.");
        optimizations.Add("Use ArrayPool<T>.Shared.Rent() instead of new byte[].");
        optimizations.Add("Use Span<T> and stackalloc for small short-lived buffers.");
    }

    // Low throughput with many users = severe blocking
    if (result.RequestsPerSecond < 5 && result.ConcurrentUsers >= 10)
    {
        issues.Add(
            $"Very low throughput: {result.RequestsPerSecond:F1} RPS with " +
            $"{result.ConcurrentUsers} concurrent users.");
        recommendations.Add(
            "Under 5 RPS with 10 users suggests severe blocking. " +
            "Check for Thread.Sleep or synchronous I/O on the hot path.");
    }

    // ... more rules for error rates, high p95, etc.

    return new AnalysisResult
    {
        Severity        = DetermineSeverity(issues),
        Issues          = issues,
        Recommendations = recommendations,
        Optimizations   = optimizations,
    };
}
```

The AI wraps this with conversational context. It knows the endpoint URL, any code you've shown it, and results from previous runs. The combination - rule-based detection + AI reasoning - is more useful than either alone.

---

## Understanding the Numbers: p50, p95, p99

If percentile latency is new to you, here's the short version.

Imagine 1,000 requests sorted by response time:
- **p50 (median):** The 500th. Half your users are faster, half are slower.
- **p95:** The 950th. 1 in 20 users is slower than this.
- **p99:** The 990th. 1% of users experience this or worse.

A healthy API has p99 about 2-5× its p50. If p99 is 20-100× the p50, some requests are catastrophically slow - and that's the ThreadPool starvation signature.

Our actual test results demonstrate this clearly:

**`/fast` (healthy baseline) - 10 users, 5 seconds:**
```
RPS:     2,395.6
Average: 4.2ms
P50:     3.8ms
P95:     7.9ms   (~2× p50 - healthy)
P99:    11.5ms   (~3× p50 - healthy)
```

**`/slow-thread-sleep` (starved) - 20 users, 15 seconds:**
```
RPS:    35.1
P50:   ~500ms
P95:  1000ms   (2× p50 - looks fine)
P99:  1002ms   (2× p50 - still looks fine?)
```

The starvation is visible in the throughput collapse (35 RPS vs 2,395), not always in the p99/p50 ratio - because every request is equally stuck waiting for a thread. The more users you add, the worse it gets.

![Load test results showing the RPS and latency comparison between the two endpoints](_screenshots/thread-sleep-results.png)

---

## Part 6: The Blazor Dashboard

The dashboard gives you a visual overview of all stored runs. It polls `/api/results` from the MCP server and renders the data with Chart.js.

![Blazor dashboard showing all test runs with color-coded severity](_screenshots/dashboard-home.png)

The Compare page puts two results side by side with percentage deltas - the same data the MCP tool returns, but in a shareable visual format.

![Dashboard compare page showing Thread.Sleep vs Task.Delay side by side](_screenshots/dashboard-compare.png)

The key design decision: the dashboard knows nothing about MCP. It's a plain REST client. The AI uses the MCP endpoint. Both read from the same `IResultStore`.

---

## Connecting to GitHub Copilot

Add `.vscode/mcp.json` to your workspace:

```json
{
  "servers": {
    "performance-lab": {
      "type": "http",
      "url": "http://localhost:5200/mcp"
    }
  }
}
```

Start all three services, open Copilot in Agent mode, and start asking:

```
Run a load test against http://localhost:5100/fast for 5 seconds with 10 users
```

```
Compare http://localhost:5100/slow-thread-sleep with http://localhost:5100/slow-task-delay
using 15 seconds and 20 concurrent users
```

```
Detect threadpool starvation for result <ID>
```

```
Generate a full performance report for the last two runs
```

![GitHub Copilot chat showing a compare_endpoints call with the full result output](_screenshots/copilot-compare-result.png)

The AI understands the context across multiple messages. After running a comparison it can explain why one endpoint is faster, suggest the code fix, and generate a report - all in the same conversation.

---

## The Broader Pattern

This project is a teaching tool. Every endpoint represents a real mistake I've seen in production .NET codebases. The analyzer's rules come from real performance investigations.

But the more important thing is the **pattern**:

> Build domain-specific MCP tools around your existing services. The AI provides the reasoning layer; your tools provide grounded, real-world data.

Performance diagnosis is a perfect use case because the AI genuinely can't tell you what's slow in your specific API without real numbers. MCP gives it real numbers. The AI gives you context, explanation, and next steps.

You could apply the same pattern to:
- A deployment tool that queries your CI/CD pipeline
- A database tool that runs EXPLAIN on slow queries
- A metrics tool that reads from your Prometheus/Grafana stack

The MCP protocol is just HTTP + a schema. The value is entirely in what you expose.

The full project is on GitHub: [link]

---

*The code shown here is simplified for readability. The full project includes error handling, cancellation support, POST endpoint testing, before/after comparisons, and a complete Blazor dashboard.*
