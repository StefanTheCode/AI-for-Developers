# RAG Basics

A minimal **Retrieval-Augmented Generation (RAG)** pipeline - so an LLM answers from *your* data instead of guessing. You add text, it gets embedded and stored as vectors in Postgres; at query time the most relevant chunks are retrieved and fed to a local Ollama model to ground the answer.

**This is the C#/.NET implementation (ASP.NET Core); the pattern is the same in any language.**

Part of the [AI in .NET Starter Kit](https://thecodeman.net/ai-in-dotnet-starter-kit) by [TheCodeMan](https://thecodeman.net).

---

## How it works (any language)

```
Your text ──▶ Ollama embeddings ──▶ store vectors in Postgres (pgvector)
                                            │
Question ──▶ embed question ──▶ retrieve top matches ──▶ Ollama LLM ──▶ grounded answer
```

1. **Ingest:** turn each piece of text into an embedding (a vector of floats) and store it next to the text in a `vector` column.
2. **Retrieve:** embed the question with the **same** model and ask Postgres for the nearest vectors (pgvector's distance operators do the ranking in SQL).
3. **Generate:** put the retrieved text into the prompt as context and tell the model to answer only from it.

Nothing here is .NET-specific: Ollama is an HTTP API, pgvector is SQL, and the prompt is a string.

## Same idea in your stack

| Component | .NET (this repo) | Python | TypeScript | Java | Go |
|---|---|---|---|---|---|
| Embeddings (Ollama) | custom `OllamaEmbeddingGenerator` | `ollama` package or plain HTTP | `ollama` npm package or `fetch` | Spring AI / LangChain4j Ollama support, or plain HTTP | Ollama HTTP API (`net/http`) |
| Vector storage | Postgres + pgvector via `TextContext` / `TextRepository` | pgvector-python with psycopg or SQLAlchemy | pgvector-node with `pg` / Drizzle / Prisma raw SQL | pgvector-java with JDBC (or Spring AI's PgVector store) | pgvector-go with pgx |
| LLM call | Ollama | `ollama` or `openai` SDK (Ollama is OpenAI-compatible) | Vercel AI SDK or `ollama` | Spring AI / LangChain4j | official `openai-go` against Ollama's OpenAI-compatible endpoint, or plain HTTP |
| API layer | ASP.NET Core minimal APIs | FastAPI | Express / Hono / Next.js route | Spring Boot | `net/http` |
| Optional framework | Semantic Kernel | LlamaIndex, LangChain | LangChain.js | LangChain4j, Spring AI | langchaingo |

---

## The .NET implementation

Key pieces:

- `EmbeddingGenerator/` - `OllamaEmbeddingGenerator` turns text into vectors via a local Ollama model.
- `Repository/` - `TextContext` + `TextRepository` store and query embeddings in Postgres.
- `Services/RagService.cs` - ties retrieval + generation together.
- `Program.cs` - minimal API endpoints (see `RagBasics.http` for sample requests).

---

## Prerequisites

- **.NET 10 SDK**
- **[Ollama](https://ollama.com)** running locally - pull the models used in the code, e.g.:
  ```bash
  ollama pull all-minilm      # embeddings
  ollama pull llama3           # or whichever LLM the code references
  ```
- **PostgreSQL with the pgvector extension** (the sample uses [Neon Serverless Postgres](https://neon.tech), but any pgvector Postgres works)

---

## Setup

1. Open `RagBasics/appsettings.json` and set your **own** Postgres connection string:

   ```json
   "ConnectionStrings": {
     "PostgreSQL": "Host=YOUR_HOST;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
   }
   ```

   > ⚠️ Do not commit real credentials. Use your own database and keep secrets out of source control (User Secrets / environment variables).

2. Make sure the `vector` extension is enabled on your database:

   ```sql
   CREATE EXTENSION IF NOT EXISTS vector;
   ```

---

## Run

```bash
cd RagBasics
dotnet run
```

Then use `RagBasics.http` (or Swagger) to add some text and ask a question. You'll see the answer grounded in the text you ingested.

---

## Want to go further?

Building production RAG, agents, and MCP servers - in .NET and beyond - is exactly what I teach in the **[AI for Developers community](https://www.skool.com/ai-for-dotnet-developers/about)** on Skool: 49 skills, 7 agents, lessons, and live office hours (7-day free trial).
