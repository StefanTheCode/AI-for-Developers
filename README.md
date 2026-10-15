# AI for Developers - Free Learning Repo

**Everything you need to learn AI as a developer, in one place: a roadmap, ready-to-use Claude Code skills, runnable AI projects, and a monthly build challenge.**

This repo covers the two things every developer now needs:

1. **Use AI to build faster** - Claude Code, CLAUDE.md, skills, agents and MCP on your real projects.
2. **Build AI into your own apps** - LLM calls, embeddings, semantic search, RAG, agents, and running it all in production.

The concepts are language-agnostic. The reference code is in C#/.NET, and every project shows how to do the same thing in Python, TypeScript, Java and Go.

Built by [Stefan Đokić - TheCodeMan](https://thecodeman.net), Microsoft MVP.

---

## What's inside

| Folder | What it is | Who it's for |
|--------|-----------|--------------|
| **[docs/roadmap.md](docs/roadmap.md)** | **AI Roadmap for Developers - 2026.** An 8-step path from "AI-curious" to shipping AI, with an "In your stack" map for .NET, Python, TypeScript, Java and Go. | Start here if you don't know where to begin |
| **[Claude](./Claude)** | **Claude Code Starter Skills.** 5 skills, a security agent and CLAUDE.md templates that work in any stack. Install as a plugin in 2 commands. | Anyone using Claude Code |
| **[MCP Server - API Performance Analysis](./MCP%20Server%20-%20API%20Performance%20Analysis)** | A Model Context Protocol server that lets Claude, Copilot or Cursor load-test and diagnose your API. | Roadmap Step 3 |
| **[Semantic Search AI Example](./Semantic%20Search%20AI%20Example)** | Search by meaning with local embeddings (Ollama) and cosine similarity. | Roadmap Step 5 |
| **[RAG Basics](./RAG%20Basics)** | A minimal RAG pipeline: embed, store vectors in Postgres (pgvector), ground the LLM's answers in your data. | Roadmap Step 6 |
| **[Learn AI in .NET](./Learn%20AI%20in%20.NET)** | **12 hands-on projects** from Starter to Expert, each with a step-by-step `BUILD-GUIDE.md` and a fully commented reference solution. | Build Challenge, .NET devs (prompts work in any language) |
| **[CHALLENGE.md](CHALLENGE.md)** | **The Build Challenge.** Pick one project, build it with AI, understand every line, ship it. | Anyone who learns by building |

---

## Where to start

- **New to AI as a developer?** Read the [roadmap](docs/roadmap.md), then install the [Claude Code starter skills](./Claude).
- **Want to build AI features?** Run [Semantic Search](./Semantic%20Search%20AI%20Example), then [RAG Basics](./RAG%20Basics), then the [MCP server](./MCP%20Server%20-%20API%20Performance%20Analysis).
- **Learn best by doing?** Pick a project in [Learn AI in .NET](./Learn%20AI%20in%20.NET) and join the [Build Challenge](CHALLENGE.md).

---

## Prerequisites

**Common (any stack):**

- **[Ollama](https://ollama.com)** running locally for embeddings and local LLMs (no API keys needed). Pull the models each project lists, e.g. `ollama pull llama3.2`, `ollama pull all-minilm`.
- **PostgreSQL with pgvector** for the RAG projects. Any pgvector-enabled Postgres works (e.g. [Neon](https://neon.tech)).
- An **MCP client** for the MCP projects: Claude Code, Claude Desktop, GitHub Copilot or Cursor.

**For the .NET reference code:**

- **.NET 10 SDK**

> **Before you run anything:** set your **own** connection strings and API keys in each project's config. Never commit real secrets.

---

## Go further

Video lessons for the roadmap drop inside the **[AI for Developers community](https://www.skool.com/ai-for-dotnet-developers/about)** on Skool (7-day free trial), together with 49 skills, 7 agents, Claude Code workflow lessons and live office hours where I answer your questions.

📬 Free: [weekly newsletter on .NET, architecture & AI](https://thecodeman.net) (21,000+ developers) · ▶️ [YouTube](https://www.youtube.com/@thecodeman_)

---

## License

Free to use, share, and adapt. If it helps you, share it with a developer who needs it. A ⭐ is appreciated.

*Built by [Stefan Đokić](https://thecodeman.net) · [LinkedIn](https://www.linkedin.com/in/djokic-stefan/) · [X](https://x.com/TheCodeMan__)*
