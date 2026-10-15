# 09 · Smart Invoice Parser

> **Advanced project** · Focus: **Structured output** + **validating AI answers**.

Reads messy, free-form invoice text and asks an LLM to extract it into a **typed
`Invoice`** object (structured output). Then — crucially — it **validates** the
result with plain C# math, because an LLM's numbers are a starting point, not the
truth. Includes an **offline demo** so the validation lesson runs even without Ollama.

---

## What you'll learn

| Concept | Where to look |
|---|---|
| **Structured output** — the type becomes the schema | [`Services/InvoiceExtractor.cs`](src/InvoiceParser/Services/InvoiceExtractor.cs), [`Models/Invoice.cs`](src/InvoiceParser/Models/Invoice.cs) |
| Steering extraction with `[Description]` hints | [`Models/Invoice.cs`](src/InvoiceParser/Models/Invoice.cs) |
| **Validating AI answers** with deterministic checks | [`Services/InvoiceValidator.cs`](src/InvoiceParser/Services/InvoiceValidator.cs) |
| Catching hallucinated / misread numbers | [`Services/InvoiceValidator.cs`](src/InvoiceParser/Services/InvoiceValidator.cs) |
| Failing gracefully when the model is unavailable | [`Program.cs`](src/InvoiceParser/Program.cs) |

---

## Prerequisites (for the live path)

**[Ollama](https://ollama.com)** running locally with a chat model:

```bash
ollama pull llama3.2
```

> The **offline validator demo runs without any of this** — it always executes so
> you can see the validation logic immediately.

---

## Run it

```bash
cd "09 - Smart Invoice Parser"
dotnet run --project src/InvoiceParser
```

You'll see:

1. **Offline demo** — the validator catching a known-bad extraction (wrong line
   total + wrong grand total).
2. **Live extraction** (if Ollama is up) — a clean invoice that passes, and a
   deliberately inconsistent invoice (`365 + 73 ≠ 448`) that the validator flags.

---

## Why validation is the whole point

Structured output makes the model return clean, typed data — but *clean* isn't the
same as *correct*. The model might misread `25.00` as `30.00`, drop a line, or
round tax. So we treat its output as **untrusted input** and check it with math it
can't fake:

- `quantity × unitPrice == lineTotal` for every line
- `sum(lineTotals) == subtotal`
- `subtotal + tax == total`

This "trust, but verify" pattern applies to **any** AI extraction task: pull the
structure with the LLM, then confirm it with deterministic code before you act on
it. The validator is a pure function — no AI, fully unit-testable.

---

## 🤖 Try these prompts with your AI assistant

**Add auto-correction (carefully):**
> "When the validator finds that line totals sum correctly but the stated subtotal
> is wrong, add an OPTIONAL correction step that fixes the subtotal and records
> what it changed. Never silently overwrite — always report corrections."

**Handle real files:**
> "Add support for reading invoices from `.txt`/`.pdf` files in a folder. For PDFs,
> extract text first. Keep the extractor and validator unchanged. Plan first."

**Write the tests:**
> "Create an xUnit test project for `InvoiceValidator` covering: a valid invoice,
> a wrong line total, a wrong subtotal, a wrong grand total, and an empty invoice.
> No AI needed — the validator is pure."

**Practise the review skill:**
> "Review my validation rules. What real-world invoice cases would slip through
> (discounts, shipping, multi-currency, rounding)? List gaps before any code."

> 💡 **The mindset:** ask for a *plan before edits*, give *outcomes + constraints*,
> and treat every AI-extracted value as untrusted until your code has checked it.

---

## Next

That's the **Advanced** tier done 🎉 — up next is the **Expert** tier:
Your Own MCP Server, AI Code Review Agent, and AI App in Production.
