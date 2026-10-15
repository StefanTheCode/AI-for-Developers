# Learn AI in .NET - 12 Projects

A hands-on series of small, fully commented .NET projects. **Build them with AI, understand every line.** Each one focuses on a couple of core skills and ships with two files:

- **`BUILD-GUIDE.md`** - build it yourself, step by step: prompts to use, "Check yourself" questions, where AI usually gets it wrong, and experiments to break it on purpose.
- **`README.md`** - the finished, fully commented reference solution. Don't open the code until you're stuck or done.

> Not a .NET developer? The reference solutions are C#, but every prompt, trap and break-it experiment works in any language. Build it in your stack.

Doing the **[Build Challenge](../CHALLENGE.md)**? Pick one project and ship it by the deadline.

---

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

> Projects **07-12** use a local LLM via **[Ollama](https://ollama.com)**. Pull the models each README lists (e.g. `ollama pull llama3.2`, `ollama pull all-minilm`). Every AI project also runs an **offline demo**, so you can see the core lesson even without a model.

**Requirements:** .NET 10 SDK, plus Ollama for projects 07-12.

---

Back to the **[main repo](../README.md)** · Join the **[AI for Developers community](https://www.skool.com/ai-for-dotnet-developers/about)** for walkthroughs and help when you get stuck.
