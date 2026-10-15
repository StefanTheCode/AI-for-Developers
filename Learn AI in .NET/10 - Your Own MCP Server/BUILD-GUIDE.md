# 10 · Your Own MCP Server — Build Guide

> **Level:** Expert · **Time:** 4–8 hours · **You'll need:** .NET 10 SDK, an MCP client (VS Code + GitHub Copilot agent mode, or Claude Desktop), Node.js for the MCP Inspector
>
> Part of the **[October Build Challenge](../../CHALLENGE.md)**. The [README](README.md) explains the finished project. This guide is for building it yourself with AI.

---

## The goal

Expose a normal C# service as **MCP tools**, so Claude or Copilot can use your API in plain language.

**The real goal:** you're no longer only *using* AI tools, you're *building for* them. The tool description is now your API contract, and the reader is a model.

---

## Step-by-step

### Step 1 — Project + SDK

```bash
dotnet new console -n NotesMcp -o src/NotesMcp
dotnet add src/NotesMcp package ModelContextProtocol --prerelease
dotnet add src/NotesMcp package Microsoft.Extensions.Hosting
```

> ⚠️ **Version note:** this repo's project pins `ModelContextProtocol 0.3.0-preview.2`. The C# SDK has since shipped **stable releases**. Check [NuGet](https://www.nuget.org/packages/ModelContextProtocol/) and use the latest stable (drop `--prerelease`). Method names changed between previews, so tell your AI assistant exactly which version you're on.

### Step 2 — Your "API" (nothing AI about it)

A thread-safe `NoteStore` with Add / GetAll / Get / Search / Delete. `ConcurrentDictionary` + `Interlocked.Increment` for ids.

### Step 3 — stdout is sacred

> **Prompt:** "Set up `Host.CreateApplicationBuilder` for an MCP server with the stdio transport. Route ALL console logging to stderr. Explain why that's mandatory for stdio."

### Step 4 — Tools

> **Prompt:** "Create a `[McpServerToolType]` class `NoteTools` that takes `NoteStore` via DI and exposes add_note, list_notes, get_note, search_notes, delete_note with `[McpServerTool]`. Write `[Description]`s for every tool AND parameter as if the reader is a very literal junior dev."

Then **rewrite the descriptions yourself**. This is the part that decides whether the model uses your tools correctly.

### Step 5 — Register + test with the Inspector

`.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()`, then:

```bash
npx @modelcontextprotocol/inspector dotnet run --project src/NotesMcp
```

Call each tool by hand before you let a model near it.

### Step 6 — Connect a real client

Use the `.vscode/mcp.json` in this folder (Copilot agent mode) or `claude_desktop_config.json` (see the README), then ask: *"Add a note called Groceries with milk and eggs, then list my notes."*

---

## ⚠️ Where AI usually gets it wrong here

| AI tends to... | Why it's wrong | What to do instead |
|---|---|---|
| `Console.WriteLine` for debugging | Corrupts JSON-RPC on stdout, and the client silently disconnects | Log to stderr |
| Generate code for an older preview API | Attributes/extension methods were renamed, so it won't compile | Pin the version, check the SDK docs |
| One-word descriptions ("Deletes note") | The model guesses wrong: which id? what does it return? | Say what, when, the params, and the return value |
| Expose destructive tools with no guard | The model can delete everything from one misread prompt | Confirmations, scopes, soft delete |
| Return huge payloads | Floods the model's context | Summaries + a separate "get details" tool |
| Let exceptions bubble up raw | The model gets a stack trace and loops | Clear error text the model can act on |

### 🔍 Review moment: our own code

`NoteStore` is in-memory, and with stdio **the client starts a new process** each session. Close VS Code and your notes are gone. Is that a bug or a design choice? Persist them with EF Core + SQLite without changing a single tool signature.

---

## 🔨 Break it on purpose

1. Add `Console.WriteLine("debug")` inside `add_note`. Call it from the client. What happens?
2. Change `delete_note`'s description to *"Does stuff with notes"*. Ask the model to "clean up my notes". What does it call?
3. Ask the model to add a note with 50,000 characters. Nothing stops it. Add a limit.

---

## ✅ Definition of done

- [ ] All 5 tools work in the MCP Inspector
- [ ] A real client (Copilot or Claude) uses them from plain-language requests
- [ ] On the latest stable `ModelContextProtocol` package
- [ ] Nothing writes to stdout except the protocol
- [ ] Input limits + readable errors on every tool
- [ ] You wrote down **one thing the AI got wrong**

## 🚀 Stretch goal

**Wrap your own API from project 02 (To-Do) or 05 (Blog) as MCP tools.** That's the real skill: taking something you already built and making it usable by AI. Bonus: switch to the HTTP transport (see [MCP Server - API Performance Analysis](../../MCP%20Server%20-%20API%20Performance%20Analysis)).

---

**Doing the [October Build Challenge](../../CHALLENGE.md)?** Share your repo and the one thing AI got wrong in **[AI for Developers](https://www.skool.com/ai-for-dotnet-developers/about)**. Advanced & Expert builders get walkthroughs, roadmaps, and someone to ask when they get stuck.
