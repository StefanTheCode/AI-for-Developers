using Microsoft.Extensions.AI;
using OllamaSharp;

// =============================================================================
// AI Support Chatbot — Advanced project #7
// Focus: Microsoft.Extensions.AI (IChatClient) + streaming responses.
//
// A console support bot for a fictional product ("Contoso Cloud"). It keeps the
// conversation history, sends it to a local LLM via Ollama, and STREAMS the reply
// token-by-token so it feels responsive.
//
// Prerequisite: Ollama running locally with a chat model pulled, e.g.
//   ollama pull llama3.2
// =============================================================================

// Config via environment variables (with sensible defaults) — no extra packages.
var ollamaUrl = Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://127.0.0.1:11434";
var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2";

// LESSON — program against the abstraction, not the vendor.
// `IChatClient` is from Microsoft.Extensions.AI. Today it's backed by Ollama;
// switching to OpenAI or Azure OpenAI later is a one-line change here, and the
// rest of the app (history, streaming loop) stays identical.
IChatClient chat = new OllamaApiClient(new Uri(ollamaUrl), model);

// The system message sets the bot's role and boundaries. It's the cheapest,
// highest-leverage way to steer an LLM — no fine-tuning required.
const string systemPrompt = """
    You are "Nimbus", the friendly support assistant for Contoso Cloud, a developer
    platform that hosts web apps and databases.
    - Be concise and practical. Prefer short steps over long essays.
    - If a question is outside Contoso Cloud (e.g. general trivia), politely steer
      back to how you can help with the product.
    - If you don't know something, say so and suggest where the user could look.
    Never invent pricing, SLAs, or features you're unsure about.
    """;

// The full conversation. We resend it every turn so the model has context —
// LLMs are stateless; "memory" is just the message list you pass in.
var history = new List<ChatMessage>
{
    new(ChatRole.System, systemPrompt)
};

Console.WriteLine("💬 Nimbus — Contoso Cloud support bot");
Console.WriteLine($"   model: {model}  ·  server: {ollamaUrl}");
Console.WriteLine("   Commands: /reset (new conversation), /exit (quit)\n");

while (true)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("You: ");
    Console.ResetColor();

    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    switch (input.Trim().ToLowerInvariant())
    {
        case "/exit" or "/quit":
            Console.WriteLine("Bye! 👋");
            return;

        case "/reset":
            history.RemoveRange(1, history.Count - 1); // keep the system message
            Console.WriteLine("(conversation reset)\n");
            continue;
    }

    history.Add(new ChatMessage(ChatRole.User, input));

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("Nimbus: ");
    Console.ResetColor();

    try
    {
        // STREAMING: instead of waiting for the whole answer, we print each chunk
        // as it arrives. We also accumulate the text so we can store the full
        // reply back into the history for the next turn.
        var reply = new System.Text.StringBuilder();

        await foreach (var update in chat.GetStreamingResponseAsync(history))
        {
            Console.Write(update.Text);
            reply.Append(update.Text);
        }

        Console.WriteLine("\n");
        history.Add(new ChatMessage(ChatRole.Assistant, reply.ToString()));
    }
    catch (HttpRequestException ex)
    {
        // Most common cause: Ollama isn't running or the model isn't pulled.
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n⚠️  Couldn't reach the model: {ex.Message}");
        Console.WriteLine($"    Is Ollama running? Try:  ollama pull {model}  then  ollama serve\n");
        Console.ResetColor();

        // Drop the user turn we couldn't answer so history stays consistent.
        history.RemoveAt(history.Count - 1);
    }
}
