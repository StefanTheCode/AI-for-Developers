using System.Diagnostics;
using AiInProduction.Cost;
using Microsoft.Extensions.AI;

namespace AiInProduction.Eval;

/// <summary>
/// Runs a suite of <see cref="EvalCase"/>s against a chat client and scores each.
///
/// For every case it measures latency, reads token usage, estimates cost, and
/// applies the scorer — the three things you care about in production: is it
/// CORRECT (eval), is it FAST (latency), and what does it COST (tokens).
/// </summary>
public sealed class EvalRunner(IChatClient chat, string model)
{
    public async Task<EvalResult> RunAsync(EvalCase testCase, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await chat.GetResponseAsync(testCase.Prompt, cancellationToken: ct);

        stopwatch.Stop();

        // Token usage isn't guaranteed by every provider; default to 0 if absent.
        var inputTokens = response.Usage?.InputTokenCount ?? 0;
        var outputTokens = response.Usage?.OutputTokenCount ?? 0;
        var cost = TokenCostCalculator.Estimate(model, inputTokens, outputTokens);

        var (passed, detail) = Scorers.KeywordMatch(response.Text, testCase.ExpectedKeywords);

        return new EvalResult(testCase.Name, passed, detail, stopwatch.Elapsed, inputTokens, outputTokens, cost);
    }

    public async Task<IReadOnlyList<EvalResult>> RunAllAsync(IEnumerable<EvalCase> cases, CancellationToken ct = default)
    {
        var results = new List<EvalResult>();
        foreach (var testCase in cases)
            results.Add(await RunAsync(testCase, ct));
        return results;
    }
}
