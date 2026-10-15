# Semantic Search

Search that understands **meaning**, not just keywords. This console example embeds a list of blog-post titles with a local Ollama model, then ranks them by **cosine similarity** to your query - so "how do I make my database faster" surfaces the performance and EF Core posts even when the words don't match.

**This is the C#/.NET implementation; the pattern is the same in any language.**

Part of the [AI in .NET Starter Kit](https://thecodeman.net/ai-in-dotnet-starter-kit) by [TheCodeMan](https://thecodeman.net).

---

## How it works (any language)

1. Turn each title into a vector (an **embedding**) once, using an embedding model (`all-minilm` via Ollama).
2. At runtime, turn the user's query into a vector with the **same** model.
3. Score every title by **cosine similarity** to the query vector and sort descending.
4. The top results are the best semantic matches.

Cosine similarity is a few lines of math in any language, and Ollama's embedding endpoint is plain HTTP + JSON. When the list grows beyond memory, move the vectors into a vector store (pgvector, Qdrant) - see **RAG Basics**.

## Same idea in your stack

| Component | .NET (this repo) | Python | TypeScript | Java | Go |
|---|---|---|---|---|---|
| Embedding abstraction | `Microsoft.Extensions.AI` (`IEmbeddingGenerator`) | `ollama` / `openai` SDK, or LangChain embeddings | Vercel AI SDK (`embed`, `embedMany`) | Spring AI / LangChain4j embedding models | Ollama HTTP API or `openai-go` |
| Ollama connection | `OllamaSharp` | `ollama` package | `ollama` npm package or AI SDK Ollama provider (community) | Spring AI / LangChain4j Ollama support | `net/http` |
| Cosine similarity | `System.Numerics.Tensors` (`TensorPrimitives`) | NumPy | a small loop (or a math library) | a small loop | a small loop |
| At scale | pgvector / Qdrant | pgvector-python / qdrant-client | pgvector-node / Qdrant JS client | pgvector-java / Qdrant Java client | pgvector-go / Qdrant Go client |

---

## The .NET implementation

- **`Microsoft.Extensions.AI`** - the unified AI abstractions for .NET (`IEmbeddingGenerator`, `Embedding<T>`).
- **`OllamaSharp`** - connects to a local Ollama instance and generates embeddings with the `all-minilm` model.
- **`System.Numerics.Tensors`** - `TensorPrimitives` computes cosine similarity efficiently.

Each title is turned into a vector once; your query is turned into a vector at runtime; the closest vectors (by cosine similarity) are the best semantic matches.

---

## Prerequisites

- **.NET 10 SDK**
- **[Ollama](https://ollama.com)** running locally with the embedding model:
  ```bash
  ollama pull all-minilm
  ```
  The code connects to Ollama at `http://127.0.0.1:11434` - change the URI in `Program.cs` if yours differs.

---

## Run

```bash
cd SemanticSearch
dotnet run
```

Enter a query when prompted and watch the titles get ranked by semantic relevance. Try queries that don't share any words with the titles - that's where semantic search beats keyword search.

---

## Next step

Semantic search is the foundation for RAG - see the **[RAG Basics](../RAG%20Basics)** module to feed retrieved results to an LLM. And to build production AI features and agents in your stack, join the **[AI for Developers community](https://www.skool.com/ai-for-dotnet-developers/about)** on Skool (7-day free trial).
