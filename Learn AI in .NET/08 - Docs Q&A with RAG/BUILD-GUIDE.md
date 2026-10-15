# 08 · Docs Q&A with RAG — Build Guide

> **Level:** Advanced · **Time:** 6–10 hours · **You'll need:** .NET 10 SDK, [Ollama](https://ollama.com) with `all-minilm` + `llama3.2`, an AI assistant
>
> Part of the **[October Build Challenge](../../CHALLENGE.md)**. The [README](README.md) explains the finished project. This guide is for building it yourself with AI.

---

## The goal

Ask questions about your own documents and get answers grounded in them, with sources. Chunk → embed → search → generate, with no database.

**The real goal:** knowing that when RAG answers badly, it's almost always **retrieval**, not the LLM, and knowing how to debug it.

---

## Step-by-step

### Step 1 — Your knowledge base

Write 3–5 short markdown docs about a fictional product (or reuse `knowledge/`). Include **specific facts** (default region, limits, retention days) so you can check the answers.

### Step 2 — Chunking

> **Prompt:** "Write a `DocumentChunker` that splits markdown on blank lines and merges small paragraphs until a chunk has at least 200 characters. Each chunk keeps its source file name. Explain the trade-off between big and small chunks."

### Step 3 — Embeddings

> **Prompt:** "Using `IEmbeddingGenerator<string, Embedding<float>>` backed by Ollama's `all-minilm`, embed all chunks in ONE batch call."

**Check yourself:** Why must the question be embedded with the *same* model as the documents?

### Step 4 — Vector store

> **Prompt:** "Build an in-memory vector store: a list of (chunk, vector) pairs and `Search(queryVector, topK)` that ranks by cosine similarity with `TensorPrimitives.CosineSimilarity`. Explain what the score means."

### Step 5 — Ask

Embed the question → top 3 chunks → put them in the prompt with *"answer ONLY from the context, otherwise say exactly: I don't know based on the provided documents."* → return the answer **and** the sources with scores.

### Step 6 — Print the sources, every time

You can't debug RAG without seeing what was retrieved.

---

## ⚠️ Where AI usually gets it wrong here

| AI tends to... | Why it's wrong | What to do instead |
|---|---|---|
| Embed whole documents | One blurry vector per doc, so imprecise search | Chunk first |
| Put *all* docs in the prompt and call it RAG | That's context stuffing: it doesn't scale and costs more | Retrieve top-K |
| Write cosine similarity by hand, without normalizing | Scores are wrong and ranking is off | `TensorPrimitives.CosineSimilarity` |
| Skip the "I don't know" instruction | The model answers from its training data and sounds sure | A strict grounded prompt |
| Hide the retrieved chunks | You can't tell a bad answer from bad retrieval | Always show sources + scores |
| Embed chunks one by one in a loop | N network calls instead of 1 | Batch |

### 🔍 Review moment: our own code

`Search` **always** returns the top 3, even when the best score is 0.1. For *"What's the capital of France?"* the model still gets 3 irrelevant chunks and is trusted to say "I don't know". Add a similarity threshold and short-circuit before the LLM call. How do you pick the threshold? (Hint: log the scores for 10 good and 10 bad questions.)

Also: the chunker has a **minimum** size but no **maximum**. What happens with one 5,000-character paragraph?

---

## 🔨 Break it on purpose

1. Ask something that isn't in the docs. Look at the similarity scores.
2. Add a doc that **contradicts** another one (the default region is "EU-West" in one and "US-East" in the other). Which answer wins?
3. Set `MinChunkLength` to 20, then 2000. Compare the retrieved sources for the same question.
4. Index with `all-minilm`, then query with a different embedding model. What do the scores look like?

---

## ✅ Definition of done

- [ ] Answers are correct for 5 questions you wrote *before* running it
- [ ] Out-of-scope questions get "I don't know"
- [ ] A similarity threshold is in place, and the LLM isn't called when nothing clears it
- [ ] Sources + scores are printed for every answer
- [ ] You can explain embeddings to a non-AI dev in 2 sentences
- [ ] You wrote down **one thing the AI got wrong**

## 🚀 Stretch goal

Swap the in-memory store for **pgvector** (see [RAG Basics](../../RAG%20Basics)) or `Microsoft.Extensions.VectorData`, and return citations with the exact sentence used.

---

**Doing the [October Build Challenge](../../CHALLENGE.md)?** Share your repo and the one thing AI got wrong in **[AI for Developers](https://www.skool.com/ai-for-dotnet-developers/about)**. Advanced & Expert builders get walkthroughs, roadmaps, and someone to ask when they get stuck.
