# 12 · AI App in Production — Build Guide

> **Level:** Expert · **Time:** 8–15 hours · **You'll need:** .NET 10 SDK, [Ollama](https://ollama.com), Docker (optional, for the Aspire dashboard), an AI assistant
>
> Part of the **[October Build Challenge](../../CHALLENGE.md)**. The [README](README.md) explains the finished project. This guide is for building it yourself with AI.

---

## The goal

Take an AI app **you already built** (your chatbot from 07, RAG from 08, or invoice parser from 09) and make it production-grade: evals, cost tracking, and telemetry.

**The real goal:** *"That last one will teach you more than the other eleven together."* Production is where you find out if your AI feature actually works: measured, not assumed.

> This repo's project shows the three pieces on a toy eval suite. Your job is to apply them to a real app.

---

## Step-by-step

### Step 1 — Write the eval set BEFORE touching code

10–20 real inputs for your app, each with what a good answer must (and must not) contain. For RAG: questions with known answers + out-of-scope questions that must return "I don't know". For the invoice parser: invoices with known correct totals.

**Check yourself:** If you can't write down what "correct" means, how will you know if a prompt change helped?

### Step 2 — Deterministic scorers

> **Prompt:** "Create `EvalCase` / `EvalResult` records and scorers in plain C#: exact match, contains-all, must-not-contain, and a JSON-schema check. Each scorer returns (passed, detail). No LLM-as-judge yet."

### Step 3 — Eval runner

> **Prompt:** "Build an `EvalRunner` that runs each case through my `IChatClient`, measures latency with `Stopwatch`, reads `response.Usage` (which can be null), estimates cost, and scores the result. Print a table plus pass rate, total tokens, and total cost."

### Step 4 — Cost model

A price table per model (input vs. output per 1M tokens). **Look up current prices on your provider's pricing page.** Don't trust the numbers an AI gives you.

### Step 5 — OpenTelemetry

> **Prompt:** "Wrap my chat client with `.UseOpenTelemetry(sourceName: "MyApp")`. Add a TracerProvider and MeterProvider for that source with a console exporter. Then show me how to switch to OTLP → Aspire dashboard."

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

### Step 6 — Get a baseline, then change ONE thing

Run the suite and save the numbers. Now change the prompt, the model, *or* the top-K, one at a time. Did quality go up? Did cost?

---

## ⚠️ Where AI usually gets it wrong here

| AI tends to... | Why it's wrong | What to do instead |
|---|---|---|
| Hardcode model prices from its training data | Prices change, and your cost report is fiction | Your provider's current pricing page |
| Write keyword scorers that are too loose | `"4"` matches `"14"`, `"build"` matches `"I can't build that"`: evals that pass garbage | Exact/regex/schema checks + a case that *must* fail |
| Set `EnableSensitiveData = true` and forget it | Prompts + user data (PII) end up in your trace backend | Off in prod, or redact |
| Jump straight to LLM-as-judge | Slow, costly, non-deterministic | Deterministic scorers first |
| Report average latency | Hides the slow tail users actually feel | p50 / p95 |
| Assume `Usage` is always there | Some providers or streams don't return it → NRE | Handle null |

### 🔍 Review moment: our own code

- `Scorers.KeywordMatch` passes if **any** keyword appears anywhere. Write an eval case that *should* fail and prove the scorer fails it. If you can't make it fail, it isn't testing anything.
- `passRate` divides by `results.Count`. What happens with an empty suite?
- `TokenCostCalculator` defaults unknown models to a cheap tier. Is "guess low" the right failure mode for a cost report?

---

## 🔨 Break it on purpose

1. Add *"Explain your answer in detail"* to one eval prompt. Watch output tokens and cost jump.
2. Turn `EnableSensitiveData` on and read one span. Would you want that in your company's logs?
3. Swap `llama3.2` for another model and rerun. Did the pass rate change? Now you have a number, not a feeling.

---

## ✅ Definition of done

- [ ] A real app (07, 08, or 09) is instrumented, not just the toy suite
- [ ] 10+ eval cases, including ones that must fail
- [ ] Baseline numbers saved: pass rate, p95 latency, tokens, cost per request
- [ ] Traces visible in the Aspire dashboard (or another OTLP backend)
- [ ] Sensitive data is off (or redacted) in your "production" config
- [ ] One change made and measured against the baseline
- [ ] The eval suite runs in CI with a pass-rate threshold
- [ ] You wrote down **one thing the AI got wrong**

## 🚀 Stretch goal

Add budget guardrails: a per-request token limit and a daily cost cap that switches to a cheaper model, and an alert when the eval pass rate drops.

---

**Finished all 12?** Post your production app in **[AI for Developers](https://www.skool.com/ai-for-dotnet-developers/about)**. That's where we build the Advanced & Expert levels with source code, roadmaps, and help when you get stuck.
