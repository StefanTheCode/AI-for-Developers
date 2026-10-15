namespace AiInProduction.Eval;

/// <summary>A single evaluation case: a prompt and what a good answer must contain.</summary>
public sealed record EvalCase(string Name, string Prompt, string[] ExpectedKeywords);

/// <summary>The scored outcome of running one <see cref="EvalCase"/>.</summary>
public sealed record EvalResult(
    string Name,
    bool Passed,
    string Detail,
    TimeSpan Latency,
    long InputTokens,
    long OutputTokens,
    decimal Cost);

/// <summary>
/// Deterministic scorers for LLM output.
///
/// LESSON — evals turn "seems fine" into a number you can track.
/// Before you ship an AI feature, you need to know if a prompt change made things
/// better or worse. An eval is just: a fixed set of inputs + a way to score the
/// outputs. These scorers are plain C# (keyword + non-empty checks) so they're
/// fast, free, and repeatable. For fuzzier quality you'd add an "LLM-as-judge"
/// scorer — but always start with cheap deterministic ones.
/// </summary>
public static class Scorers
{
    /// <summary>Passes if the answer is non-empty and contains at least one expected keyword.</summary>
    public static (bool Passed, string Detail) KeywordMatch(string answer, string[] expectedKeywords)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return (false, "empty answer");

        var matched = expectedKeywords
            .Where(k => answer.Contains(k, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return matched.Length > 0
            ? (true, $"matched: {string.Join(", ", matched)}")
            : (false, $"none of [{string.Join(", ", expectedKeywords)}] found");
    }
}
