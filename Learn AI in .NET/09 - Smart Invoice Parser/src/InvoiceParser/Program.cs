using System.Text.Json;
using InvoiceParser.Models;
using InvoiceParser.Services;
using Microsoft.Extensions.AI;
using OllamaSharp;

// =============================================================================
// Smart Invoice Parser — Advanced project #9
// Focus: Structured output + validating AI answers.
//
// Reads raw invoice text, asks the LLM to extract it into a typed Invoice
// (structured output), then VALIDATES the result with plain C# math — because you
// never trust an LLM's numbers without checking them.
//
// Prerequisite for the live path: Ollama running with a chat model, e.g.
//   ollama pull llama3.2
// (An OFFLINE validator demo runs even without Ollama.)
// =============================================================================

var ollamaUrl = new Uri(Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://127.0.0.1:11434");
var chatModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2";

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

Console.WriteLine("🧾 Smart Invoice Parser\n");

// --- Always-on OFFLINE demo: the validator catching a bad extraction ---------
// This needs no AI, so it always runs and proves the validation logic.
Console.WriteLine("── Offline demo: validating a KNOWN-BAD extraction ──");
var canned = SampleData.CannedBadExtraction();
PrintValidation(InvoiceValidator.Validate(canned));
Console.WriteLine();

// --- Live path: extract real invoices with the LLM ---------------------------
IChatClient chat = new OllamaApiClient(ollamaUrl, chatModel);
var extractor = new InvoiceExtractor(chat);

var samples = new (string Name, string Text)[]
{
    ("Clean invoice", SampleData.CleanInvoice),
    ("Inconsistent invoice", SampleData.InconsistentInvoice)
};

foreach (var (name, text) in samples)
{
    Console.WriteLine($"── Extracting: {name} ──");

    Invoice? invoice;
    try
    {
        invoice = await extractor.ExtractAsync(text);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"⚠️  Skipping live extraction — Ollama unreachable: {ex.Message}");
        Console.WriteLine($"    Try:  ollama pull {chatModel}  then  ollama serve\n");
        break;
    }

    if (invoice is null)
    {
        Console.WriteLine("❌ The model's reply couldn't be bound to the Invoice schema.\n");
        continue;
    }

    Console.WriteLine("Extracted (structured output):");
    Console.WriteLine(JsonSerializer.Serialize(invoice, jsonOptions));

    Console.WriteLine("\nValidation:");
    PrintValidation(InvoiceValidator.Validate(invoice));
    Console.WriteLine();
}

Console.WriteLine("Done.");

// --- helpers -----------------------------------------------------------------

static void PrintValidation(IReadOnlyList<string> issues)
{
    if (issues.Count == 0)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Passed — the numbers are internally consistent.");
        Console.ResetColor();
        return;
    }

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"⚠️  {issues.Count} issue(s) found:");
    foreach (var issue in issues)
        Console.WriteLine($"   • {issue}");
    Console.ResetColor();
}
