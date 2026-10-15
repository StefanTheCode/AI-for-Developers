using DocsQa.Services;
using Microsoft.Extensions.AI;
using OllamaSharp;

// =============================================================================
// Docs Q&A with RAG — Advanced project #8
// Focus: Embeddings + vector search (Retrieval-Augmented Generation).
//
// Loads a small knowledge base about the fictional "Contoso Cloud", embeds it,
// and answers your questions grounded ONLY in those documents.
//
// Prerequisite: Ollama running locally with:
//   ollama pull all-minilm     (embeddings)
//   ollama pull llama3.2       (chat / answer generation)
// =============================================================================

var ollamaUrl = new Uri(Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://127.0.0.1:11434");
var embedModel = Environment.GetEnvironmentVariable("OLLAMA_EMBED_MODEL") ?? "all-minilm";
var chatModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2";

// One Ollama client backs embeddings, another backs chat — both via Microsoft.Extensions.AI.
IEmbeddingGenerator<string, Embedding<float>> embedder = new OllamaApiClient(ollamaUrl, embedModel);
IChatClient chat = new OllamaApiClient(ollamaUrl, chatModel);

var store = new InMemoryVectorStore();
var rag = new RagPipeline(embedder, chat, store);

// --- Load the knowledge base -------------------------------------------------
var knowledgeDir = Path.Combine(AppContext.BaseDirectory, "knowledge");
var documents = Directory
    .EnumerateFiles(knowledgeDir, "*.md")
    .Select(path => (Source: Path.GetFileName(path), Content: File.ReadAllText(path)))
    .ToList();

Console.WriteLine("📚 Docs Q&A with RAG");
Console.WriteLine($"   Loaded {documents.Count} documents from /knowledge");

try
{
    Console.WriteLine("   Embedding documents (this calls Ollama)...");
    await rag.IngestAsync(documents);
    Console.WriteLine($"   Indexed {store.Count} chunks. Ask away!\n");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\n⚠️  Couldn't reach Ollama for embeddings: {ex.Message}");
    Console.WriteLine($"    Try:  ollama pull {embedModel}  and  ollama pull {chatModel}\n");
    return;
}

Console.WriteLine("Sample questions:");
Console.WriteLine("  • How do I deploy a web app and which region is the default?");
Console.WriteLine("  • What are the rate limits?");
Console.WriteLine("  • How long are database backups kept?");
Console.WriteLine("  • What's the capital of France?   (should say it doesn't know)\n");

while (true)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Question (empty to quit): ");
    Console.ResetColor();

    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question))
        break;

    try
    {
        var (answer, sources) = await rag.AskAsync(question);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\nAnswer: {answer}");
        Console.ResetColor();

        // Showing the retrieved sources builds trust and helps you debug retrieval.
        Console.WriteLine("\nRetrieved from:");
        foreach (var (chunk, score) in sources)
            Console.WriteLine($"  • {chunk.Source} (similarity {score:F3})");
        Console.WriteLine();
    }
    catch (HttpRequestException ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n⚠️  Model call failed: {ex.Message}\n");
        Console.ResetColor();
    }
}

Console.WriteLine("Bye! 👋");
