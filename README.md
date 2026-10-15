# AI Roadmap for Developers - 2026

**The only guide you need to go from "AI-curious" to shipping real AI - in whatever language you write.**

No hype. No random tool lists. A clear path covering the two things every developer now needs: **using AI to build faster** (Claude Code, agents, skills, MCP) and **building AI features into your own apps** (LLMs, embeddings, RAG, agents). Concepts first, then an "In your stack" map for .NET, Python, TypeScript, Java and Go.

👉 **[Read the full roadmap →](docs/roadmap.md)**

Built by [Stefan Đokić - TheCodeMan](https://thecodeman.net), Microsoft MVP.

---

## Who This Is For

- **Developers in any language** who keep hearing about AI but don't know where it fits in real work
- **Backend engineers** who want to build AI features into their own apps
- **Teams** trying to actually use AI coding tools well, not just install them

---

## The Path

```
Step 1: AI coding assistants - get faster today      → Week 1
Step 2: Skills, agents & custom workflows            → Week 2
Step 3: MCP - connect AI to your tools               → Week 3
Step 4: LLMs in your stack - your first AI feature   → Week 4
Step 5: Embeddings & semantic search                 → Week 5
Step 6: RAG - ground the AI in your data             → Week 6
Step 7: AI agents                                    → Week 7
Step 8: Production AI (evals, cost, guardrails)      → Week 8
```

Two tracks: **Use AI** (Steps 1-3) and **Build AI** (Steps 4-8). Each step builds on the last. Don't skip ahead.

---

## Companion code in this repo

The roadmap isn't just reading - several steps have runnable code right here to build from. The projects are written in **C#/.NET**, but the concepts transfer directly: each project README has a **"Same idea in Python/TypeScript/Java/Go"** table so you can build it in your stack.

| Folder | Roadmap step | What it is |
|--------|--------------|-----------|
| **[Claude](./Claude)** | Steps 1-2 | Claude Code **skills + an agent + CLAUDE.md templates**. Stack-agnostic: the skills work in any language (with a .NET example template included). |
| **[MCP Server - API Performance Analysis](./MCP%20Server%20-%20API%20Performance%20Analysis)** | Step 3 | A Model Context Protocol server (C#) that lets AI clients (Copilot, Claude, Cursor) diagnose API performance. |
| **[Semantic Search AI Example](./Semantic%20Search%20AI%20Example)** | Step 5 | Search by *meaning* - local embeddings with Ollama + cosine similarity (C#). |
| **[RAG Basics](./RAG%20Basics)** | Step 6 | A minimal RAG pipeline - embed, store vectors in Postgres, ground the LLM's answers in your data (C#). |

Each project is a self-contained solution with its own README.

---

## 🏗️ October Build Challenge

**Pick one of the 12 projects below, build it with AI, and finish it by October 31.** Every project has a `BUILD-GUIDE.md` with step-by-step prompts, "where AI gets it wrong" traps, break-it experiments, and a definition of done.

👉 **[Start the challenge →](CHALLENGE.md)**

---

## C# Projects for the AI Era

> **Reference solutions are in C#/.NET; the BUILD-GUIDE prompts work in any language - build it in your stack.**

A hands-on series of small, fully-commented .NET projects - **build them with AI,
understand every line.** Each one focuses on a couple of core skills and ships
with a README full of prompts you can practise on. Built in four tiers:

**01 · Starter**

| Folder | Focus |
|--------|-------|
| **[01 · Expense Tracker CLI](./01%20-%20Expense%20Tracker%20CLI)** | LINQ + collections · reading & fixing AI code |
| **[02 · To-Do REST API](./02%20-%20To-Do%20REST%20API)** | Minimal APIs · EF Core + SQLite |
| **[03 · Weather Dashboard](./03%20-%20Weather%20Dashboard)** | `HttpClient` · `async`/`await` |

**02 · Intermediate**

| Folder | Focus |
|--------|-------|
| **[04 · URL Shortener](./04%20-%20URL%20Shortener)** | Caching · rate limiting |
| **[05 · Blog API with Auth](./05%20-%20Blog%20API%20with%20Auth)** | JWT auth · validation + tests |
| **[06 · Background Job Runner](./06%20-%20Background%20Job%20Runner)** | Hosted services · channels + retries |

**03 · Advanced** *(local AI via [Ollama](https://ollama.com) - no API keys)*

| Folder | Focus |
|--------|-------|
| **[07 · AI Support Chatbot](./07%20-%20AI%20Support%20Chatbot)** | `Microsoft.Extensions.AI` · streaming |
| **[08 · Docs Q&A with RAG](./08%20-%20Docs%20Q%26A%20with%20RAG)** | Embeddings · vector search |
| **[09 · Smart Invoice Parser](./09%20-%20Smart%20Invoice%20Parser)** | Structured output · validating AI answers |

**04 · Expert**

| Folder | Focus |
|--------|-------|
| **[10 · Your Own MCP Server](./10%20-%20Your%20Own%20MCP%20Server)** | Expose your API to Claude & Copilot (MCP, stdio) |
| **[11 · AI Code Review Agent](./11%20-%20AI%20Code%20Review%20Agent)** | The agent loop · tool calling |
| **[12 · AI App in Production](./12%20-%20AI%20App%20in%20Production)** | Evals + token costs · OpenTelemetry |

> Projects **07-12** use a local LLM via **[Ollama](https://ollama.com)** (no API keys). Pull the models each README lists (e.g. `ollama pull llama3.2`, `ollama pull all-minilm`). Every AI project also runs an **offline demo** so you can see the core lesson even without a model.

---

## Prerequisites

**Common (any stack):**

- **[Ollama](https://ollama.com)** running locally (embeddings + local LLMs) - pull the models each module lists (e.g. `ollama pull all-minilm`)
- **PostgreSQL with pgvector** - any pgvector-enabled Postgres works (e.g. [Neon](https://neon.tech))
- An **MCP client** for the MCP module: Claude Code, Claude Desktop, GitHub Copilot, or Cursor

**For the .NET reference code:**

- **.NET 10 SDK**

> **Before you run anything:** open each module's `appsettings.json` and set your **own** connection strings / API keys. Never commit real secrets.

---

## 🎥 A mini-course is coming

I'm recording this roadmap step by step as short video clips - a full mini-course walking through every stage. New clips drop inside the **[AI for Developers community](https://www.skool.com/ai-for-dotnet-developers/about)** on Skool (7-day free trial), where you also get 49 skills, 7 agents, lessons, and live office hours where I answer your questions.

📬 Or follow along free: [weekly newsletter on .NET, architecture & AI](https://thecodeman.net) (21,000+ developers) · ▶️ [YouTube](https://www.youtube.com/@thecodeman_)

---

## License

Free to use, share, and adapt. If it helps you, share it with a developer who needs it. A ⭐ is appreciated.

*Built by [Stefan Đokić](https://thecodeman.net) · [LinkedIn](https://www.linkedin.com/in/djokic-stefan/) · [X](https://x.com/TheCodeMan__)*
