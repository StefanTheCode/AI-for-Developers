# 10 · Your Own MCP Server

> **Expert project** · Focus: **Expose your API to Claude & Copilot** via the Model Context Protocol.

A small "Notes" service exposed as **MCP tools**, so an AI client (GitHub Copilot,
Claude Desktop, Cursor) can create, list, search, and delete notes for you — in
plain language. It uses the **stdio** transport: the AI client launches this
process and talks to it over stdin/stdout.

---

## What you'll learn

| Concept | Where to look |
|---|---|
| Registering an MCP server + stdio transport | [`Program.cs`](src/NotesMcp/Program.cs) |
| Turning service methods into **MCP tools** | [`Tools/NoteTools.cs`](src/NotesMcp/Tools/NoteTools.cs) |
| Writing tool metadata the model can actually use | [`Tools/NoteTools.cs`](src/NotesMcp/Tools/NoteTools.cs) |
| Why logging must go to **stderr** on stdio | [`Program.cs`](src/NotesMcp/Program.cs) |
| DI in tools (your API stays normal C#) | [`Services/NoteStore.cs`](src/NotesMcp/Services/NoteStore.cs) |

> The repo's other MCP project, **[MCP Server - API Performance Analysis](../../MCP%20Server%20-%20API%20Performance%20Analysis)**,
> uses the **HTTP** transport. This one uses **stdio** — the two ways to run an MCP server.

---

## The tools this server exposes

| Tool | What it does |
|---|---|
| `add_note` | create a note (title + content) |
| `list_notes` | list all notes, newest first |
| `get_note` | fetch one note by id |
| `search_notes` | find notes containing text |
| `delete_note` | delete a note by id |

---

## Run / connect it

You don't run this by hand — an MCP client launches it. But you can verify it
builds:

```bash
cd "10 - Your Own MCP Server"
dotnet build
```

### VS Code (GitHub Copilot)

A ready-to-use [`.vscode/mcp.json`](.vscode/mcp.json) is included. Copy it to your
workspace's `.vscode/mcp.json`, then run **MCP: List Servers → notes → Start**.
Now ask Copilot Chat (Agent mode): *"add a note titled 'Groceries' with milk and
eggs, then list my notes."*

### Claude Desktop

Add this to `claude_desktop_config.json`
(`%APPDATA%\Claude\` on Windows, `~/Library/Application Support/Claude/` on macOS):

```json
{
  "mcpServers": {
    "notes": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/10 - Your Own MCP Server/src/NotesMcp"]
    }
  }
}
```

Restart Claude Desktop; the `notes` tools appear in the tools menu.

---

## The details that trip people up

- **stdout is sacred.** On stdio, stdout carries the JSON-RPC protocol. If you
  `Console.WriteLine` or log to stdout, you corrupt it and the client silently
  disconnects. That's why `Program.cs` routes **all logs to stderr**.
- **Tool descriptions are the contract.** The model decides whether to call a tool
  from its `[Description]`. Write them like docs for a literal junior dev — say
  what it does, what each parameter means, and what it returns.
- **Tools are just DI methods.** `NoteTools` takes `NoteStore` via the constructor,
  exactly like a controller. Your existing services need no changes to be exposed.

---

## 🤖 Try these prompts with your AI assistant

**Persist the notes:**
> "Swap the in-memory `NoteStore` for EF Core + SQLite so notes survive restarts.
> Keep the tool signatures identical. Show the plan first."

**Add an MCP resource:**
> "Besides tools, expose the notes as an MCP *resource* (read-only) so clients can
> browse them without a tool call. Explain resources vs. tools."

**Harden it:**
> "Add input validation and length limits to the tools, and return structured
> errors the model can understand. What happens today if `add_note` gets a
> 10 MB body?"

**Practise the review skill:**
> "Review my tool descriptions as if you were the LLM. Which are ambiguous? Rewrite
> the weakest one and explain why the model would misuse the original."

> 💡 **The mindset:** ask for a *plan before edits*, give *outcomes + constraints*,
> and remember the model only knows what your tool metadata tells it.

---

## Next

Move on to **[11 · AI Code Review Agent](../11%20-%20AI%20Code%20Review%20Agent)** — the agent loop + tool calling.
