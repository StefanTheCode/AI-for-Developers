using Microsoft.Extensions.AI;

namespace DocsQa.Services;

/// <summary>
/// The RAG pipeline: <b>R</b>etrieval-<b>A</b>ugmented <b>G</b>eneration.
///
/// Two phases:
///   1. INGEST  — chunk docs, embed each chunk, store the vectors.
///   2. ASK     — embed the question, find the closest chunks (retrieval), then
///                ask the LLM to answer using ONLY those chunks (generation).
///
/// LESSON — RAG grounds the model in YOUR data.
/// The LLM never saw Contoso Cloud's docs during training. By retrieving the
/// relevant chunks and pasting them into the prompt, we let a general model answer
/// specific questions accurately — and we tell it to say "I don't know" when the
/// answer isn't in the context, which curbs hallucination.
/// </summary>
public sealed class RagPipeline(
    IEmbeddingGenerator<string, Embedding<float>> embedder,
    IChatClient chat,
    InMemoryVectorStore store)
{
    /// <summary>Chunks and embeds every document, filling the vector store.</summary>
    public async Task IngestAsync(IEnumerable<(string Source, string Content)> documents)
    {
        var chunks = documents
            .SelectMany(doc => DocumentChunker.Chunk(doc.Source, doc.Content))
            .ToList();

        // Batch-embed all chunk texts in one call (far faster than one-by-one).
        var texts = chunks.Select(c => c.Text).ToList();
        var embeddings = await embedder.GenerateAsync(texts);

        for (var i = 0; i < chunks.Count; i++)
            store.Add(chunks[i], embeddings[i].Vector);
    }

    /// <summary>Answers a question using retrieved context. Returns the answer and its sources.</summary>
    public async Task<(string Answer, IReadOnlyList<(Chunk Chunk, float Score)> Sources)> AskAsync(string question)
    {
        // --- RETRIEVAL ---
        var queryEmbedding = await embedder.GenerateAsync([question]);
        var matches = store.Search(queryEmbedding[0].Vector, topK: 3);

        var context = string.Join(
            "\n\n---\n\n",
            matches.Select(m => $"[{m.Chunk.Source}]\n{m.Chunk.Text}"));

        // --- GENERATION ---
        // The system prompt forces the model to stay grounded in the context.
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, """
                You are a documentation assistant. Answer the user's question using
                ONLY the context provided below. If the answer is not in the context,
                reply exactly: "I don't know based on the provided documents."
                Be concise and cite the source file name in brackets when you can.
                """),
            new(ChatRole.User, $"Context:\n{context}\n\nQuestion: {question}")
        };

        var response = await chat.GetResponseAsync(messages);
        return (response.Text, matches);
    }
}
