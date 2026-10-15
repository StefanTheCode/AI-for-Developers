# The AI Roadmap for Developers - 2026

**The only guide you need to go from "AI-curious" to shipping real AI, in whatever language you write.**

> The concepts in this roadmap are the same in C#, Python, TypeScript, Java and Go. Each "Build AI" step ends with an **In your stack** table that maps the concept to real libraries. The companion code in this repo is C#/.NET - one first-class example, not a requirement.

---

## Introduction: Why Most Developers Get AI Wrong

Let's be honest. Most developers approach AI in one of two broken ways.

**Group 1 - the hype-chasers.** They read about transformers, fine-tuning, and the newest framework every week. They watch 30 hours of "AI for developers" videos. They can talk about attention mechanisms. But they can't add a single working AI feature to their app, and their coding assistant still writes generic code that ignores their conventions.

**Group 2 - the skeptics.** They tried an AI assistant once, got mediocre output, and decided "AI is overhyped." They're now quietly falling behind developers who ship 2x faster.

**Both are wrong.** The truth in 2026 is simple:

> AI won't replace good developers. But developers who can **use AI to build faster** and **build AI into their products** are already pulling ahead - in output, in what they can ship alone, and in what they get paid.

### What This Roadmap Does Differently

This roadmap is built around one idea: **learn by building, in the right order - concepts first, then your stack.**

It runs on two tracks:
- **Track A - Use AI (Steps 1-3):** ship faster starting today, in any language.
- **Track B - Build AI (Steps 4-8):** put AI features into your own apps.

Most developers start on Track A (immediate wins in your day job) and move into Track B (the skills that make you rare).

> This repo has companion code for several steps - the `Semantic Search`, `RAG Basics`, and `MCP Server` projects (C#/.NET), plus the `toolkit/` with 45 skills and 7 agents (stack-agnostic). You build, not just read. Each project README also shows the same idea in Python, TypeScript, Java and Go.

**The single mindset for this whole roadmap:** AI is a tool you *drive*, not magic you *wait for*. You stay the engineer. It goes faster.

### Why the concepts transfer

LLM providers are HTTP APIs. Embeddings are arrays of floats. A vector store is a database. MCP is a protocol with official SDKs in many languages. None of it is tied to one runtime. Learn the concept once, and switching stacks is a library swap, not a re-learn.

---

## The Real AI Journey

Follow these steps **in order**. Don't skip ahead.

---

### Step 1 - AI Coding Assistants: Get Faster Today
**Week 1 · Track A**

**What you're building:** A daily workflow where an AI assistant writes code *your way* - matching your stack, architecture, and conventions on the first try.

**What you need to learn:**
- Pick **one** assistant and go deep: **Claude Code**, **GitHub Copilot**, or **Cursor**.
- The **`CLAUDE.md` / rules file** at your repo root - the single biggest quality lever. It tells the AI your stack, architecture, and conventions so it stops writing generic code (the "average" folder structure, patterns your team doesn't use) and writes *yours*.
- Prompting for real work: describe the **outcome and constraints**, not the steps. Ask for a **plan before edits**. Tell it what to **never** do.
- Reviewing AI output critically - you're still responsible for every line.

**What to ignore for now:**
- Trying all three assistants side by side (later, not now).
- Editor-specific power-user tricks and keybindings.
- Local/self-hosted coding models.

**Real-world example:** You ask for one endpoint and get back extra layers, a mapping library you don't use, and someone else's folder structure. Reasonable code - just not *your* code. A `CLAUDE.md` fixes exactly this: the assistant reads it automatically at the start of every session and follows it. (In a .NET codebase that might mean "no repositories, no AutoMapper, use our feature folders"; in a Node codebase "Zod at the edges, no classes for DTOs" - same idea.)

**Common mistakes:**
- No context file → generic output → you blame the AI.
- Accepting code you don't understand.
- Vague prompts ("make this better") instead of outcomes ("return a result type, not exceptions; keep it in the existing feature folder").

**You're ready to move on when:**
- The AI matches your project's style on the first try, and you know why.
- You have a `CLAUDE.md` in a real project.
- You completed one real backlog task fully with the assistant.

**Classification:**
| Topic | Priority |
|---|---|
| An AI assistant (Claude Code / Copilot / Cursor) | MUST LEARN |
| `CLAUDE.md` / rules file | MUST LEARN |
| Outcome-based prompting | MUST LEARN |
| Plan-before-edit workflow | MUST LEARN |
| Trying every assistant | OPTIONAL |

> **Companion code:** [`toolkit/templates/`](../toolkit/templates) has a generic `CLAUDE-md-template.md` for any stack, plus a filled-in .NET example.

---

### Step 2 - Skills, Agents & Custom Workflows
**Week 2 · Track A**

**What you're building:** You turn a general assistant into a *specialist for your codebase* by giving it reusable capabilities.

**What you need to learn:**
- **Skills** - focused, reusable instructions the AI loads automatically when your request matches ("optimize this ORM query", "scaffold an endpoint", "write integration tests").
- **Agents / subagents** - specialists that explore your whole codebase and produce a report (architecture review, security audit, code review).
- **Slash commands & hooks** (Claude Code) - repeatable workflows and automation around tool use.
- Writing **one** custom skill for a task you repeat weekly.

**What to ignore for now:**
- Collecting 50 skills you'll never use.
- Complex multi-agent orchestration.
- Building an entire skill library before you've *used* enough of them.

**Real-world example:** Instead of re-explaining "how we write database queries here" every session, a skill carries that checklist permanently. You say "optimize this query" and it applies your standards automatically - whether the query is EF Core, SQLAlchemy, Prisma, or Hibernate.

**Common mistakes:**
- Hoarding skills you never reach for. Start with the 5 that match your daily work.
- Writing skills before understanding what a good one looks like.

**You're ready to move on when:**
- You have a small set of skills you actually reach for.
- You've written one of your own.
- You've run an agent (e.g. a security or code-review agent) on a real repo.

**Classification:**
| Topic | Priority |
|---|---|
| Using skills | MUST LEARN |
| Using agents / subagents | MUST LEARN |
| Writing a custom skill | MUST LEARN |
| Slash commands & hooks | USE WHEN NEEDED |
| Multi-agent orchestration | OPTIONAL |

> **Companion code:** the [`toolkit/`](../toolkit) folder has 45 ready, stack-agnostic skills, 7 agents, and CLAUDE.md templates to start from.

---

### Step 3 - MCP: Connect AI to Your Tools
**Week 3 · Track A → B bridge**

**What you're building:** An MCP server that exposes some of your own operations as tools any AI client can call - the bridge from "AI that writes code" to "AI that can *do things* in your systems."

**What you need to learn:**
- What **MCP (Model Context Protocol)** is and why it exists - one protocol, many clients (Claude, Copilot, Cursor).
- **Using** existing MCP servers from your client.
- **Building** a simple MCP server with an **official MCP SDK**. SDKs exist for **C#, TypeScript, Python, Java, Kotlin and Go** (and more) - pick the one for your stack. In C# you define tools with `[McpServerTool]`; the other SDKs have their own equivalent (decorators, registration functions).
- The **stdio** transport (local) and how the client launches your server.

**What to ignore for now:**
- HTTP / remote MCP transport and hosting (start local).
- Auth, advanced tool schemas, and resource/prompt primitives.

**Real-world example:** You give an AI client a "run a load test" or "check this service's health" tool. Now the assistant doesn't just *describe* a fix - it can gather the data itself.

**Common mistakes:**
- **Logging to stdout on a stdio server** - it corrupts the protocol. Log to **stderr**. (True in every language.)
- Exposing write/destructive operations without guardrails.
- Over-broad tools instead of small, well-described ones.

**You're ready to move on when:**
- Your AI client can call a tool you wrote.
- You understand the difference between using and building an MCP server.

**Classification:**
| Topic | Priority |
|---|---|
| Understanding MCP | MUST LEARN |
| Using MCP servers | MUST LEARN |
| Building an MCP server in your language (stdio) | MUST LEARN |
| HTTP / remote transport | USE WHEN NEEDED |
| Auth & advanced schemas | OPTIONAL |

> **Companion code:** the [`MCP Server - API Performance Analysis`](../MCP%20Server%20-%20API%20Performance%20Analysis) project is a full working MCP server in C# to study and extend. Its README maps each piece to the TypeScript, Python, Java and Go SDKs.

---

### Step 4 - LLMs in Your Stack: Your First AI Feature
**Week 4 · Track B**

**What you're building:** One clean LLM call in your app, behind an abstraction, so you can swap providers without rewriting anything.

**What you need to learn:**
- A **provider-agnostic abstraction** for chat calls (see the table below - every major stack has one).
- Calling a provider: **OpenAI**, **Anthropic**, **Azure OpenAI**, or **Ollama** (local, free) for dev.
- **Streaming** responses and **structured output** (JSON validated into a typed object).
- Keeping the API key in config, cancellation on every call, and a timeout.

```csharp
// C# - one call, provider-agnostic (Microsoft.Extensions.AI)
IChatClient client = /* OpenAI / Azure OpenAI / Ollama */;
var response = await client.GetResponseAsync("Summarize this ticket: ...", cancellationToken: ct);
```

```ts
// TypeScript - same idea with the Vercel AI SDK; swap the provider, keep the call
import { generateText } from "ai";
const { text } = await generateText({ model, prompt: "Summarize this ticket: ...", abortSignal });
```

**What to ignore for now:**
- Fine-tuning your own model (you almost never need it).
- Supporting multiple providers at once.
- Prompt-engineering "hacks."

**Real-world example:** Summarize a support ticket, classify an email, or draft a reply - one LLM call inside an existing feature. Behind an abstraction, you can start on Ollama for free and switch to a hosted model in production by config.

**Common mistakes:**
- Scattering one provider's SDK everywhere instead of putting calls behind one interface.
- No timeout / cancellation.
- Sending secrets or PII into the prompt.

**You're ready to move on when:**
- You can add an LLM call to any app in your stack, swap the provider by config, and stream the result.
- You can get structured JSON back and validate it safely.

**In your stack:**
| Stack | Chat abstraction / SDKs | Structured output |
|---|---|---|
| .NET | `Microsoft.Extensions.AI` (`IChatClient`); official OpenAI and Anthropic .NET SDKs; OllamaSharp | Deserialize into a C# type (M.E.AI has typed-response helpers) |
| Python | Official `openai` and `anthropic` SDKs; `ollama`; LangChain or LiteLLM (optional) for a multi-provider layer | Pydantic models |
| TypeScript | Vercel AI SDK (`ai`); official `openai` and `@anthropic-ai/sdk` | Zod schemas |
| Java | Spring AI (`ChatClient`); LangChain4j; official OpenAI and Anthropic Java SDKs | Map to Java records / POJOs (both frameworks support it) |
| Go | Official `openai-go` and `anthropic-sdk-go`; langchaingo (optional) | JSON schema + `encoding/json` into structs |

> Tip: Ollama exposes an OpenAI-compatible endpoint, so most OpenAI SDKs can talk to a local model just by changing the base URL.

**Classification:**
| Topic | Priority |
|---|---|
| A provider-agnostic chat abstraction | MUST LEARN |
| OpenAI / Anthropic / Azure OpenAI / Ollama | MUST LEARN |
| Streaming & structured output | MUST LEARN |
| Multiple providers at once | USE WHEN NEEDED |
| Fine-tuning | OPTIONAL |

---

### Step 5 - Embeddings & Semantic Search
**Week 5 · Track B**

**What you're building:** Search that understands **meaning**, not keywords - the foundation of everything "AI over your data."

**What you need to learn:**
- What an **embedding** is; generating them with a local model (Ollama `all-minilm`) or a hosted one.
- **Cosine similarity** and why it ranks results.
- Storing/querying vectors in a **vector store**: **pgvector** (Postgres) or **Qdrant** are great defaults.
- Keeping the embedding model **the same** for indexing and querying.

**What to ignore for now:**
- Advanced index tuning (HNSW parameters).
- Hybrid (keyword + vector) search.
- Building your own vector store.

**Real-world example:** A user searches "how do I make my database faster" and your search surfaces the ORM-tuning and performance articles - even though none of those words appear in the query.

**Common mistakes:**
- Mismatched embedding models between indexing and querying.
- Storing full entities instead of just the text + id you need.
- Treating cosine distance thresholds as universal (they're data-dependent).

**You're ready to move on when:**
- You can embed text, store it, and retrieve the most *relevant* results for a query.
- You can explain, in one sentence, why semantic search beats keyword search.

**In your stack:**
| Stack | Embeddings | pgvector client | Qdrant client |
|---|---|---|---|
| .NET | `Microsoft.Extensions.AI` (`IEmbeddingGenerator`); OllamaSharp | `Pgvector` (+ EF Core / Npgsql integration) | `Qdrant.Client` |
| Python | `openai`, `ollama`, or `sentence-transformers` | `pgvector` (pgvector-python, works with psycopg / SQLAlchemy) | `qdrant-client` |
| TypeScript | Vercel AI SDK (`embed`), `openai`, `ollama` | `pgvector` (pgvector-node) | `@qdrant/js-client-rest` |
| Java | Spring AI or LangChain4j embedding models | pgvector-java (Spring AI also has a PgVector store) | Qdrant Java client |
| Go | `openai-go`, Ollama HTTP API | pgvector-go | Qdrant Go client |

> Ollama's embedding endpoint is plain HTTP + JSON, so it works from any language even without an SDK.

**Classification:**
| Topic | Priority |
|---|---|
| Embeddings | MUST LEARN |
| Cosine similarity | MUST LEARN |
| Vector store (pgvector / Qdrant) | MUST LEARN |
| Hybrid search | USE WHEN NEEDED |
| HNSW index tuning | OPTIONAL |

> **Companion code:** the [`Semantic Search AI Example`](../Semantic%20Search%20AI%20Example) project runs a working semantic search (C#) you can extend or port.

---

### Step 6 - RAG: Ground the AI in Your Data
**Week 6 · Track B**

**What you're building:** A system that retrieves the relevant chunks of *your* data and lets the LLM answer from them instead of guessing. This is the most in-demand applied-AI skill right now.

**What you need to learn:**
- The pipeline: **chunk → embed → store → retrieve top-k → feed to the LLM**.
- The 3 decisions that matter most: **chunking strategy, embedding model, and top-k** (how many chunks you retrieve).
- Grounding the prompt (system prompt + retrieved context) and returning **source attribution**.

```
Your docs ──▶ chunk ──▶ embed ──▶ store (pgvector/Qdrant)
                                        │
Question ──▶ embed ──▶ retrieve top-k ──┴──▶ LLM (with context) ──▶ grounded answer + sources
```

**What to ignore for now:**
- Re-ranking, query rewriting, agentic retrieval (level 2).
- Fine-tuning (RAG replaces the need for it in most cases).

**Real-world example:** A "chat with your docs" feature: ingest your company's docs, ask a question, get an answer grounded in the actual documents - with links back to the source.

**Common mistakes:**
- Chunks too big or too small. Start ~500 tokens with overlap, then tune.
- No source attribution - users won't trust ungrounded answers.
- Dumping everything into the prompt instead of retrieving the *relevant* parts.

**You're ready to move on when:**
- Your app answers questions from *your* documents, not the model's training data.
- You can explain how chunking and top-k affect answer quality.

**In your stack:**
| Stack | Build it yourself with | Optional RAG frameworks |
|---|---|---|
| .NET | Step 4 + Step 5 libraries (`IChatClient` + `IEmbeddingGenerator` + pgvector/Qdrant) | Semantic Kernel; Microsoft.Extensions.VectorData |
| Python | Official SDK + pgvector-python / qdrant-client | LlamaIndex, LangChain |
| TypeScript | Vercel AI SDK + pgvector-node / Qdrant JS client | LangChain.js, LlamaIndex.TS |
| Java | Spring AI or LangChain4j + a vector store | Both include RAG building blocks |
| Go | Official SDK + pgvector-go / Qdrant Go client | langchaingo |

> Build the first version by hand (it's ~100 lines in any language). Reach for a framework once you know what it's doing for you.

**Classification:**
| Topic | Priority |
|---|---|
| The RAG pipeline | MUST LEARN |
| Chunking + top-k tuning | MUST LEARN |
| Grounding + source attribution | MUST LEARN |
| Re-ranking / query rewriting | USE WHEN NEEDED |
| Agentic retrieval | OPTIONAL |

> **Companion code:** the [`RAG Basics`](../RAG%20Basics) project is a minimal RAG pipeline in C#/.NET you can grow into a document Q&A.

---

### Step 7 - AI Agents
**Week 7 · Track B**

**What you're building:** An agent that decides *which tools to call* to accomplish a goal - reasoning plus action, not just a single response.

**What you need to learn:**
- **Tool calling / function calling**: give the LLM your own functions as tools.
- The **agent loop**: model picks a tool → you run it → feed the result back → repeat until done. Write it by hand once; then use a framework (table below) for planning, tools, and memory.
- **Guardrails:** agents should *suggest and act within limits*, not run wild. Human-in-the-loop for anything destructive.
- Combining an agent with your **MCP tools (Step 3)** and **RAG (Step 6)**.

**What to ignore for now:**
- Multi-agent systems and complex orchestration.
- Long-term autonomous agents (fragile and risky today).

**Real-world example:** An agent that triages an incoming error: reads the stack trace, searches your docs (RAG), checks a service via an MCP tool, and proposes a fix - but waits for you to approve any change.

**Common mistakes:**
- Over-scoping. A narrow agent that does one thing well beats a "do everything" agent that's unreliable.
- No limits on what tools can do.
- Letting the agent act without a human check on destructive operations.

**You're ready to move on when:**
- You have an agent that picks tools and completes a real, bounded task safely.
- You understand where the human stays in the loop.

**In your stack:**
| Stack | Tool calling | Agent frameworks (pick one) |
|---|---|---|
| .NET | `Microsoft.Extensions.AI` function invocation; official SDKs | Semantic Kernel, Microsoft Agent Framework |
| Python | Official `openai` / `anthropic` SDKs | OpenAI Agents SDK, Claude Agent SDK, LangGraph, Pydantic AI |
| TypeScript | Vercel AI SDK tools; official SDKs | Vercel AI SDK agents, OpenAI Agents SDK, Claude Agent SDK, Mastra |
| Java | Spring AI / LangChain4j tool support | Spring AI, LangChain4j |
| Go | Official SDKs | langchaingo (optional) - a hand-written loop is often enough |

**Classification:**
| Topic | Priority |
|---|---|
| Tool / function calling | MUST LEARN |
| One agent framework in your stack | MUST LEARN |
| Guardrails & human-in-the-loop | MUST LEARN |
| Multi-agent systems | OPTIONAL |
| Autonomous long-running agents | OPTIONAL |

---

### Step 8 - Production AI: Evals, Cost & Guardrails
**Week 8 · Track B**

**What you're building:** The difference between a demo and a product. You make your AI feature measurable, affordable, and safe.

**What you need to learn:**
- **Evaluation:** how do you know a change made it *better*? Basic evals / golden test cases for AI outputs.
- **Cost & tokens:** measure token usage, cache where you can, and pick the **right model per task** (don't use the biggest model for everything).
- **Guardrails & security:** **prompt injection** is the new SQL injection - never trust retrieved/user text as instructions. Validate tool inputs. Keep secrets/PII out of prompts and logs.
- **Observability:** trace LLM calls, latency, and failures with **OpenTelemetry** (it has SDKs for every major language).

**What to ignore for now:**
- Self-hosting models (until scale demands it).
- Heavy eval frameworks (start with a handful of golden cases).

**Real-world example:** You change a prompt and want to ship it. Without evals, you're guessing. With 20 golden test cases, you can prove it improved (or didn't) before it hits users - and your cost log shows it didn't 3x your bill.

**Common mistakes:**
- Shipping prompt changes with no way to measure regression.
- Using the biggest, most expensive model for every task.
- Trusting retrieved document text as instructions (prompt injection).

**You're ready to move on when:**
- You can measure quality and cost.
- You can explain how your AI feature resists prompt injection.

**In your stack:**
| Stack | Evals (start simple) | Observability |
|---|---|---|
| .NET | Golden cases in xUnit/NUnit; Microsoft.Extensions.AI.Evaluation | OpenTelemetry .NET |
| Python | Golden cases in pytest | OpenTelemetry Python |
| TypeScript | Golden cases in Vitest/Jest | OpenTelemetry JS |
| Java | Golden cases in JUnit | OpenTelemetry Java |
| Go | Golden cases with `go test` | OpenTelemetry Go |

> Language-neutral options also exist (e.g. promptfoo runs evals from a config file against any HTTP endpoint). But a test file with 20 inputs and checks is a perfectly good start.

**Classification:**
| Topic | Priority |
|---|---|
| Basic evals | MUST LEARN |
| Cost & token control | MUST LEARN |
| Prompt-injection guardrails | MUST LEARN |
| OpenTelemetry for AI | USE WHEN NEEDED |
| Self-hosting models | OPTIONAL |

---

## What NOT to Learn (What Most Devs Waste Time On)

This section might save you months.

### 1. Fine-tuning your own models
You almost never need it. For "AI that knows your data," **RAG** is faster, cheaper, and easier to keep up to date. Learn fine-tuning only if you have a narrow, stable task where RAG genuinely isn't enough.

### 2. The math of transformers
Interesting. Not required to ship. You can build production AI features without ever deriving attention. Learn the theory later, out of curiosity - not before you've shipped anything.

### 3. The framework of the week
The ecosystem churns weekly. The **concepts** are stable: LLM calls, embeddings, RAG, agents, MCP. Learn the concepts; the frameworks are just implementations you can swap.

### 4. Prompt-engineering "hacks"
Good context (`CLAUDE.md`, clear system prompts) and clear intent beat clever tricks. "Act as a senior 10x engineer" is not a strategy.

### 5. Chasing benchmarks and model leaderboards
Pick a capable model, ship, and measure on *your* task with *your* evals. The best model on a benchmark is often not the best (or cheapest) for your use case.

### 6. Building your own vector database / agent framework
Use pgvector or Qdrant. Use the established agent framework for your stack. Your value is in the *application*, not in re-implementing infrastructure.

### 7. Switching languages "to do AI"
You don't need to move to Python to build AI features. Every major stack has official SDKs and solid libraries. Build AI where your product already lives.

---

## How AI Fits Into an App (Mental Model)

You don't bolt "AI" onto an app. You add specific, bounded capabilities. Here's where they live - the same in any backend (ASP.NET Core, FastAPI, Express, Spring Boot, a Go service):

```
                    ┌──────────────────────────────┐
User request ─────▶ │  Your backend app            │
                    │                              │
                    │   Chat abstraction ──▶ LLM   │  ← Step 4: a single AI call
                    │      ▲                       │
                    │      │ context               │
                    │   Retrieval (embeddings) ────┼──▶ Vector store  ← Steps 5-6: RAG
                    │      ▲                       │
                    │   Agent (tool calling) ──────┼──▶ Your tools / MCP  ← Steps 3, 7
                    └──────────────────────────────┘
```

**The rules of thumb:**
- **Abstract the model** behind one interface (`IChatClient` in .NET, the AI SDK in TypeScript, Spring AI's `ChatClient` in Java, your own thin wrapper anywhere) so you can swap providers.
- **Ground with retrieval** (RAG) whenever the answer must come from *your* data.
- **Give the model tools** (function calling / MCP) when it needs to *act*, not just talk.
- **Keep a human in the loop** for anything that writes, deletes, or spends money.
- **Treat all retrieved/user text as untrusted** - it can contain injection.

---

## Portfolio Projects

Not toy projects. Each solves a real problem and proves a real skill. Three of them ship as runnable C#/.NET code in this repo - use them as a reference design and build yours in your own stack if you prefer.

### Project 1 - Semantic Search API
**Steps:** 4-5 · **In repo:** `Semantic Search AI Example/`
**Problem it solves:** search your content by meaning, not keywords.
**Teaches:** embeddings, cosine similarity, vector storage, a provider-agnostic AI abstraction.
**Make it senior-level:** add hybrid (keyword + vector) search, cache embeddings, expose it as a clean API with pagination, and benchmark query latency.

### Project 2 - Document Q&A (RAG)
**Step:** 6 · **In repo:** `RAG Basics/`
**Problem it solves:** ask questions of your own documents and get grounded answers.
**Teaches:** the full RAG pipeline, chunking, retrieval, prompt grounding.
**Make it senior-level:** add source citations, re-ranking, a "no answer found" path, and evals that check answer quality against a golden set.

### Project 3 - MCP Server
**Step:** 3 · **In repo:** `MCP Server - API Performance Analysis/` (C#)
**Problem it solves:** expose your app's operations as tools any AI client can use.
**Teaches:** an official MCP SDK, tool design, stdio transport.
**Make it senior-level:** add HTTP transport + hosting, input validation and auth on tools, and a read-only vs write tool split.

### Project 4 - An AI Agent
**Steps:** 7-8
**Problem it solves:** automate one real, bounded job (error triage, PR review, or support answers).
**Teaches:** tool calling, an agent framework, guardrails, combining RAG + MCP.
**Make it senior-level:** add evals, a cost log, prompt-injection guarding, and human-in-the-loop approval for any action.

Each one is a LinkedIn post, a portfolio piece, and a real skill.

---

## Production Mindset for AI

This is what separates a demo from something a company will pay for.

### Abstract the model - because providers change
Put every call behind one interface (`IChatClient`, the Vercel AI SDK, Spring AI, or your own wrapper). Today you're on Ollama for dev; tomorrow it's a hosted model in prod. Never scatter one vendor's SDK across your codebase.

### Ground answers - because hallucinations get you fired
If an answer must be correct and specific to your data, **retrieve it (RAG)** and cite the source. A confident wrong answer is worse than "I don't know."

### Guard against prompt injection - the new SQL injection
Retrieved documents and user input are **data, not instructions.** Never let them silently steer the model or trigger tools.

```
// Treat retrieved context as untrusted content, clearly separated from your instructions.
// Validate every tool input. Require human approval for anything destructive.
```

### Measure quality with evals - because "it feels better" isn't proof
Keep a small set of golden test cases (input → expected quality). Run them before shipping a prompt or model change. This is your regression test suite for AI.

### Control cost - because tokens are money
Log token usage per feature. Cache where you can. Use a small model for simple tasks and the big one only where it earns its price.

### Observe it - because you can't fix what you can't see
Trace LLM calls, latency, and failures. In 2026, OpenTelemetry is the standard in every major language - but start with good logging and a cost counter.

---

## The 8-Week Plan

Assumes ~1-2 hours a day. Adjust to your pace and experience.

### Week 1 - AI assistant + `CLAUDE.md`
**Learn:** one assistant deeply; the `CLAUDE.md` file; outcome-based prompting.
**Build:** add a `CLAUDE.md` to a real project (start from `toolkit/templates/CLAUDE-md-template.md`); finish one backlog task fully with AI.
**Practice:** take three tasks and do them AI-first; notice where the context file changes output.

### Week 2 - Skills & agents
**Learn:** skills, subagents, slash commands.
**Build:** install skills (the `toolkit/` folder), use 5 on a real repo; write one custom skill.
**Practice:** run a security/code-review agent and act on its report.

### Week 3 - MCP
**Learn:** what MCP is; the official SDK for your language; stdio transport.
**Build:** run the `MCP Server` project (or the SDK's quickstart in your stack), then add one tool of your own; connect it to your AI client.
**Practice:** call your tool by name; break it and read the (stderr) logs.

### Week 4 - Your first AI feature
**Learn:** your stack's chat abstraction, streaming, structured output.
**Build:** add one AI feature (summarize/classify/draft) behind that abstraction.
**Practice:** swap the provider by config (Ollama ↔ a hosted model); get structured JSON back.

### Week 5 - Embeddings & semantic search
**Learn:** embeddings, cosine similarity, a vector store.
**Build:** run and extend the `Semantic Search` project, or port it to your stack.
**Practice:** search with queries that share no words with the results.

### Week 6 - RAG
**Learn:** the RAG pipeline; chunking; top-k; grounding.
**Build:** grow `RAG Basics` (or your own port) into a document Q&A with source citations.
**Practice:** change chunk size and top-k; watch answer quality change.

### Week 7 - Agents
**Learn:** tool calling; one agent framework in your stack; guardrails.
**Build:** a bounded agent that uses your MCP tools + RAG for one real job.
**Practice:** add a human approval step before any action.

### Week 8 - Production
**Learn:** evals, cost/tokens, prompt-injection guardrails, observability.
**Build:** add evals + a cost log + injection guarding to your RAG/agent app.
**Practice:** change a prompt and prove (with evals) it got better; check the cost delta.

**Two months, eight things shipped. That's a portfolio.**

---

## MUST vs OPTIONAL Summary

### MUST LEARN (core for using and building AI, in any stack)
- An AI assistant + `CLAUDE.md`
- Skills & agents (using them; writing one)
- MCP with your language's SDK (stdio)
- A provider-agnostic chat abstraction (e.g. `IChatClient`, Vercel AI SDK, Spring AI)
- Embeddings + a vector store
- RAG (chunk → retrieve → ground)
- Tool calling + one agent framework
- Evals, cost control, prompt-injection guardrails

### USE WHEN NEEDED
- HTTP/remote MCP transport
- Hybrid search & re-ranking
- Multiple LLM providers at once
- OpenTelemetry for AI
- Slash commands & hooks

### OPTIONAL / SPECIALIZED
- Fine-tuning
- Transformer internals
- Multi-agent orchestration
- Self-hosting models
- Building your own vector DB / agent framework

---

## If I Were Starting AI in 2026

No sugar-coating.

**Week 1-2:** I'd get *fast* first. One assistant, a solid `CLAUDE.md`, and a few skills. This alone changes your day job immediately and buys you time for everything else.

**Week 3-4:** I'd learn MCP by building one small server in my main language, then add my first real AI feature behind a provider-agnostic abstraction - on Ollama, for free.

**Week 5-6:** I'd build semantic search, then RAG. This is the moment "AI over your data" clicks, and it's the most valuable applied skill on the market.

**Week 7-8:** I'd build one bounded agent and make it production-safe with evals, cost tracking, and injection guardrails.

**What I would NOT do:**
- I wouldn't watch 30 hours of AI theory. I'd build a feature in week 4.
- I wouldn't fine-tune anything. I'd use RAG.
- I wouldn't chase the framework of the week. I'd learn the concepts and swap implementations.
- I wouldn't switch languages just for AI. I'd build it in the stack I already ship.
- I wouldn't wait to "understand AI fully." Nobody does. You learn by shipping.

**The uncomfortable truth:** most developers will spend 2026 *reading* about AI. A few will spend it *shipping* AI. The gap between those two groups will be enormous - in skill, in speed, and in what they get paid.

**Pick one step. Build it. Ship it. Repeat.**

That's the entire roadmap.

---

## Go deeper (free)

- 🧩 [AI ToolKit skills on GitHub](https://github.com/StefanTheCode/dotnet-ai-toolkit) · 📄 CLAUDE.md Generator · 🔌 MCP Server Generator

**A step-by-step mini-course** (video clips for every stage) is being recorded inside the **[AI for Developers community](https://www.skool.com/ai-for-dotnet-developers/about)** on Skool - 7-day free trial, plus 49 skills, 7 agents, lessons, and live office hours where I answer your questions.

📬 [Weekly newsletter on .NET, architecture & AI](https://thecodeman.net) (21,000+ developers) · ▶️ [YouTube](https://www.youtube.com/@thecodeman_)

*By [Stefan Đokić - TheCodeMan](https://thecodeman.net), Microsoft MVP.*
