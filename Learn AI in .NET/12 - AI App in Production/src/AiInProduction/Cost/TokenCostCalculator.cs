namespace AiInProduction.Cost;

/// <summary>Price of a model in USD per 1,000,000 tokens.</summary>
public sealed record ModelPricing(decimal InputPerMillion, decimal OutputPerMillion);

/// <summary>
/// Estimates the dollar cost of an LLM call from its token usage.
///
/// LESSON — tokens are money; track them from day one.
/// Hosted models bill per token, split into cheaper INPUT (your prompt + history)
/// and pricier OUTPUT (the model's reply). A chatbot that resends a long history
/// every turn can get expensive fast. Local models (Ollama) have no per-token fee,
/// but you still track tokens to reason about latency and context limits.
/// </summary>
public static class TokenCostCalculator
{
    // Illustrative public prices (USD / 1M tokens). Replace with your provider's real rates.
    private static readonly Dictionary<string, ModelPricing> Prices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gpt-4o"] = new(2.50m, 10.00m),
        ["gpt-4o-mini"] = new(0.15m, 0.60m),
        ["claude-3-5-sonnet"] = new(3.00m, 15.00m),
        ["llama3.2"] = new(0.00m, 0.00m) // local via Ollama — no per-token charge
    };

    public static decimal Estimate(string model, long inputTokens, long outputTokens)
    {
        // Unknown model? Assume a cheap hosted tier so cost isn't silently zero.
        var price = Prices.TryGetValue(model, out var p) ? p : Prices["gpt-4o-mini"];

        return inputTokens / 1_000_000m * price.InputPerMillion
             + outputTokens / 1_000_000m * price.OutputPerMillion;
    }

    public static bool IsKnown(string model) => Prices.ContainsKey(model);
}
