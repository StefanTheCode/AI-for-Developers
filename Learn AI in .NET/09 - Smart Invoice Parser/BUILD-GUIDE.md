# 09 · Smart Invoice Parser — Build Guide

> **Level:** Advanced · **Time:** 4–7 hours · **You'll need:** .NET 10 SDK, [Ollama](https://ollama.com) with `llama3.2` (the offline part runs without it), an AI assistant
>
> Part of the **[October Build Challenge](../../CHALLENGE.md)**. The [README](README.md) explains the finished project. This guide is for building it yourself with AI.

---

## The goal

Messy invoice text → a typed `Invoice` object via structured output → **validated with plain C# math** before anyone trusts it.

**The real goal:** *clean* output isn't *correct* output. Every AI-extracted value is untrusted input.

---

## Step-by-step

### Step 1 — The type is the prompt

> **Prompt:** "Create an `Invoice` class (number, vendor, date as ISO string, currency, line items, subtotal, tax, total) and `InvoiceLine` (description, quantity, unit price, line total). Use `decimal` for money and `[Description]` attributes to guide extraction. Use get/set properties, not positional records. Why?"

### Step 2 — Validator FIRST (no AI yet)

> **Prompt:** "Write a pure static `InvoiceValidator.Validate(Invoice)` that returns a list of issues: missing required fields, qty × unit price ≠ line total, sum of lines ≠ subtotal, subtotal + tax ≠ total. Use a 0.01 tolerance."

Test it on a hand-made bad invoice. **This part works without any model.** That's the point.

### Step 3 — Extraction

> **Prompt:** "Implement `InvoiceExtractor` with `chat.GetResponseAsync<Invoice>(messages)` and `TryGetResult`. The system prompt must say: copy numbers EXACTLY, do NOT recalculate or fix them. Return null if the reply doesn't match the schema."

**Check yourself:** Why would you *forbid* the model from fixing wrong math?

### Step 4 — Sample data

Write one **clean** invoice and one where the source document's own numbers don't add up (`365 + 73 = 448`).

### Step 5 — Program

Offline demo first (validator on a known-bad invoice), then live extraction of both samples → print the JSON → validate.

---

## ⚠️ Where AI usually gets it wrong here

| AI tends to... | Why it's wrong | What to do instead |
|---|---|---|
| Ask for JSON in text and parse it with regex/`JsonSerializer` | Brittle: markdown fences, trailing text | Structured output `GetResponseAsync<T>` |
| Let the model "fix" the totals | You can no longer detect a bad source invoice | "Copy exactly" + validate in code |
| Trust the result because it's typed | Typed ≠ true | Deterministic checks |
| Use `double` for money | Rounding errors in validation | `decimal` |
| Compare with `==` | Tax rounding (13.599 → 13.60) fails | A tolerance |
| Assume small models always follow the schema | `llama3.2` (3B) fails sometimes | Handle `null`, retry, or use a bigger model |

### 🔍 Review moment: our own code

`InvoiceValidator` checks the date with `DateOnly.TryParse(invoice.InvoiceDate, out _)`, which uses the **current culture**. We asked for ISO `YYYY-MM-DD`, so validate exactly that: `DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, ...)`.

What else slips through today? Negative quantities, an invalid currency code, and a tax that doesn't match the stated rate.

---

## 🔨 Break it on purpose

1. **The key experiment:** run the *inconsistent* invoice 5 times. Does the model ever "helpfully" change `448.00` to `438.00`? If it does, your validator passes a wrong invoice. That's the most important thing to see in this project.
2. Remove "copy numbers exactly" from the system prompt and repeat.
3. Try a bigger model (`qwen2.5:7b`) and compare how often extraction fails.

---

## ✅ Definition of done

- [ ] The offline validator demo catches every injected error
- [ ] The clean invoice passes, and the inconsistent one is flagged
- [ ] The date is validated as strict ISO
- [ ] An xUnit project for the validator: valid, bad line, bad subtotal, bad total, empty
- [ ] You ran experiment #1 and wrote down what happened
- [ ] You wrote down **one thing the AI got wrong**

## 🚀 Stretch goal

Read `.txt`/`.pdf` invoices from a folder, and send anything with validation issues to a "needs human review" list instead of silently correcting it.

---

**Doing the [October Build Challenge](../../CHALLENGE.md)?** Share your repo and the one thing AI got wrong in **[AI for Developers](https://www.skool.com/ai-for-dotnet-developers/about)**. Advanced & Expert builders get walkthroughs, roadmaps, and someone to ask when they get stuck.
