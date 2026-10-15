using System.Numerics.Tensors;

namespace DocsQa.Services;

/// <summary>
/// A tiny in-memory vector database.
///
/// LESSON — "vector search" is just nearest-neighbour by cosine similarity.
/// We store each chunk next to its embedding (a float array). To find the most
/// relevant chunks for a query, we embed the query and compare it to every stored
/// vector with <b>cosine similarity</b> (1.0 = same direction/meaning, 0 = unrelated).
///
/// This brute-force scan is perfect for a demo. At scale you'd swap it for a real
/// vector store (pgvector, Qdrant, Azure AI Search) that indexes vectors — but the
/// idea is identical.
/// </summary>
public sealed class InMemoryVectorStore
{
    private readonly List<(Chunk Chunk, ReadOnlyMemory<float> Embedding)> _items = [];

    public int Count => _items.Count;

    public void Add(Chunk chunk, ReadOnlyMemory<float> embedding) =>
        _items.Add((chunk, embedding));

    /// <summary>Returns the <paramref name="topK"/> chunks most similar to the query vector.</summary>
    public IReadOnlyList<(Chunk Chunk, float Score)> Search(ReadOnlyMemory<float> query, int topK = 3) =>
        _items
            .Select(item => (
                item.Chunk,
                Score: TensorPrimitives.CosineSimilarity(query.Span, item.Embedding.Span)))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
}
