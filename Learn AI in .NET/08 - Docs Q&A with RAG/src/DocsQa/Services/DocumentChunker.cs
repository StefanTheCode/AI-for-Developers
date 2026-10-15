namespace DocsQa.Services;

/// <summary>One retrievable piece of a document, tagged with where it came from.</summary>
public sealed record Chunk(string Source, string Text);

/// <summary>
/// Splits documents into chunks for embedding.
///
/// LESSON — chunking is a real quality lever in RAG.
/// Embed a whole document and the vector is a blurry average of everything in it,
/// so search is imprecise. Embed tiny fragments and you lose context. A middle
/// ground — a paragraph or two — usually works best. Here we split on blank lines
/// and merge very short pieces so each chunk carries a coherent idea.
/// </summary>
public static class DocumentChunker
{
    private const int MinChunkLength = 200;

    public static IReadOnlyList<Chunk> Chunk(string source, string content)
    {
        // Split into paragraphs on blank lines.
        var paragraphs = content
            .Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var chunks = new List<Chunk>();
        var buffer = "";

        foreach (var paragraph in paragraphs)
        {
            buffer = string.IsNullOrEmpty(buffer) ? paragraph : $"{buffer}\n\n{paragraph}";

            // Flush once we've accumulated enough text for a meaningful chunk.
            if (buffer.Length >= MinChunkLength)
            {
                chunks.Add(new Chunk(source, buffer));
                buffer = "";
            }
        }

        if (!string.IsNullOrWhiteSpace(buffer))
            chunks.Add(new Chunk(source, buffer));

        return chunks;
    }
}
