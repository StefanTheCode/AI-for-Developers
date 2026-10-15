# 07 · AI Support Chatbot

> **Advanced project** · Focus: **`Microsoft.Extensions.AI`** + **streaming responses**.

A console support bot ("Nimbus" for the fictional *Contoso Cloud*). It keeps the
conversation history, talks to a **local** LLM through Ollama, and **streams** the
reply token-by-token so it feels alive. All through the vendor-neutral
`IChatClient` abstraction — so you could swap Ollama for OpenAI/Azure with a
one-line change.

---

## What you'll learn

| Concept | Where to look |
|---|---|
| The `IChatClient` abstraction (vendor-neutral AI) | [`Program.cs`](src/AiChatbot/Program.cs) |
| **Streaming** with `GetStreamingResponseAsync` | [`Program.cs`](src/AiChatbot/Program.cs) |
| Managing conversation history (LLMs are stateless) | [`Program.cs`](src/AiChatbot/Program.cs) |
| Steering behaviour with a **system prompt** | [`Program.cs`](src/AiChatbot/Program.cs) |
| Failing gracefully when the model is unreachable | [`Program.cs`](src/AiChatbot/Program.cs) |

---

## Prerequisites

1. **[Ollama](https://ollama.com)** running locally.
2. A chat model pulled:
   ```bash
   ollama pull llama3.2
   ```
   (Any chat model works — `mistral`, `phi3`, `qwen2.5`, etc. Set `OLLAMA_MODEL`.)

---

## Run it

```bash
cd "07 - AI Support Chatbot"
dotnet run --project src/AiChatbot
```

Then chat:

```
You: how do I deploy a web app?
Nimbus: Here are the quick steps for Contoso Cloud...
```

Commands: `/reset` starts a fresh conversation, `/exit` quits.

Configuration via environment variables:

| Variable | Default | Meaning |
|---|---|---|
| `OLLAMA_URL` | `http://127.0.0.1:11434` | Ollama server address |
| `OLLAMA_MODEL` | `llama3.2` | chat model name |

---

## The two ideas that matter

**Streaming.** Calling `GetStreamingResponseAsync` returns an
`IAsyncEnumerable` of chunks. We print each chunk the moment it arrives instead of
waiting for the whole answer — the difference between a bot that feels frozen and
one that feels responsive. We also accumulate the chunks so the *complete* reply
goes back into history.

**History = memory.** LLMs are **stateless**. The model only "remembers" what you
send it, so we keep a `List<ChatMessage>` (starting with the system prompt) and
resend it every turn. That's also why long chats cost more tokens — the whole
history rides along each time. (`/reset` trims it back to just the system prompt.)

---

## 🤖 Try these prompts with your AI assistant

**Swap the provider (prove the abstraction):**
> "Show me how to back `IChatClient` with Azure OpenAI instead of Ollama, reading
> the endpoint/key from configuration. The chat loop must not change at all."

**Add tools / function calling:**
> "Give Nimbus a `checkServiceStatus(region)` tool using Microsoft.Extensions.AI
> function invocation, so it can answer 'is EU-West down?'. Explain how tool calls
> flow through `IChatClient`."

**Trim history smartly:**
> "Add a token-budget-aware history trimmer that keeps the system prompt + the
> most recent messages under N tokens. Explain the trade-offs vs. summarizing."

**Practise the review skill:**
> "Review this streaming loop for cancellation and partial-failure handling. What
> happens if the stream errors halfway? List issues before writing code."

> 💡 **The mindset:** ask for a *plan before edits*, give *outcomes + constraints*,
> and remember every message you keep in history is tokens you pay for.

---

## Next

Move on to **[08 · Docs Q&A with RAG](../08%20-%20Docs%20Q%26A%20with%20RAG)** — embeddings + vector search.
