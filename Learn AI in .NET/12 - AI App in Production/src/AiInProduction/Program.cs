using AiInProduction.Cost;
using AiInProduction.Eval;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// =============================================================================
// AI App in Production — Expert project #12
// Focus: Evals, token costs, and OpenTelemetry.
//
// The three things that separate a demo from a production AI feature:
//   1. EVALS         — prove quality with a repeatable scored test suite.
//   2. TOKEN COSTS   — know what every call costs before the bill arrives.
//   3. OBSERVABILITY — trace/measure calls with OpenTelemetry.
//
// Offline demos for (1) and (2) always run. The live eval run needs Ollama:
//   ollama pull llama3.2
// =============================================================================

const string TelemetrySource = "AiApp";

var ollamaUrl = new Uri(Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://127.0.0.1:11434");
var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2";

// --- OpenTelemetry: emit spans + metrics for every AI call -------------------
// The console exporters print telemetry to the terminal so you can SEE it. In
// production you'd export to Jaeger, Azure Monitor, Grafana, etc. instead.
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(TelemetrySource)
    .AddConsoleExporter()
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter(TelemetrySource)
    .AddConsoleExporter()
    .Build();

Console.WriteLine("🏭 AI App in Production\n");

// --- (1) OFFLINE demo: token cost estimation ---------------------------------
Console.WriteLine("── Token cost estimation (illustrative prices, per 1M tokens) ──");
Console.WriteLine("Assuming a request with 1,500 input + 400 output tokens:\n");
foreach (var m in new[] { "gpt-4o", "gpt-4o-mini", "claude-3-5-sonnet", "llama3.2" })
{
    var cost = TokenCostCalculator.Estimate(m, inputTokens: 1_500, outputTokens: 400);
    var note = TokenCostCalculator.IsKnown(m) ? "" : "  (unknown model — assumed cheap tier)";
    Console.WriteLine($"  {m,-20} ${cost:F6}{note}");
}
Console.WriteLine("  → local models cost $0 per token, but you still track usage.\n");

// --- (2) OFFLINE demo: how a scorer works ------------------------------------
Console.WriteLine("── Eval scorer demo (no AI needed) ──");
var (passed, detail) = Scorers.KeywordMatch("The answer is 4.", ["4", "four"]);
Console.WriteLine($"  answer 'The answer is 4.' vs expected [4, four] → {(passed ? "PASS" : "FAIL")} ({detail})\n");

// --- (3) LIVE: run the eval suite through an instrumented client -------------
// UseOpenTelemetry wraps the client so each call emits a span under "AiApp".
IChatClient chat = new ChatClientBuilder(new OllamaApiClient(ollamaUrl, model))
    .UseOpenTelemetry(sourceName: TelemetrySource, configure: o => o.EnableSensitiveData = true)
    .Build();

var runner = new EvalRunner(chat, model);

var suite = new[]
{
    new EvalCase("arithmetic", "What is 2 + 2? Reply with only the number.", ["4"]),
    new EvalCase("dotnet-cli", "What .NET CLI command builds a project? Reply with the command.", ["build"]),
    new EvalCase("csharp-records", "Which C# keyword declares an immutable reference type introduced in C# 9? One word.", ["record"])
};

Console.WriteLine($"── Running {suite.Length} evals against '{model}' (spans print below) ──\n");

IReadOnlyList<EvalResult> results;
try
{
    results = await runner.RunAllAsync(suite);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"⚠️  Couldn't reach Ollama: {ex.Message}");
    Console.WriteLine($"    Try:  ollama pull {model}  then  ollama serve");
    Console.WriteLine("    (The cost + scorer demos above ran without it.)");
    return;
}

// --- Report -------------------------------------------------------------------
Console.WriteLine("\n── Eval results ──");
Console.WriteLine($"{"Case",-16}{"Result",-8}{"Latency",-10}{"In/Out",-12}{"Cost",-12}Detail");
foreach (var r in results)
{
    Console.WriteLine(
        $"{r.Name,-16}{(r.Passed ? "PASS" : "FAIL"),-8}" +
        $"{r.Latency.TotalMilliseconds,6:F0}ms  {r.InputTokens + "/" + r.OutputTokens,-12}" +
        $"${r.Cost,-11:F6}{r.Detail}");
}

var passRate = results.Count(r => r.Passed) / (double)results.Count;
var totalCost = results.Sum(r => r.Cost);
var totalTokens = results.Sum(r => r.InputTokens + r.OutputTokens);

Console.WriteLine($"\nPass rate : {passRate:P0} ({results.Count(r => r.Passed)}/{results.Count})");
Console.WriteLine($"Tokens    : {totalTokens}");
Console.WriteLine($"Total cost: ${totalCost:F6}  (would be non-zero on a hosted model)");
Console.WriteLine("\nScroll up to see the OpenTelemetry spans printed for each call.");
