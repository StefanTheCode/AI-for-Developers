# 12 · AI App in Production

> **Expert project** · Focus: **Evals + token costs** and **OpenTelemetry**.

The three things that separate an AI demo from an AI feature you can actually run: **evals** (is it correct?), **token cost tracking** (what does it cost?), and **observability** (what happened on each call?). This project shows all three on a small eval suite that runs against a local model.

---

## What you'll learn

| Concept | Where to look |
|---|---|
| Estimating the cost of every LLM call from token usage | [`Cost/TokenCostCalculator.cs`](src/AiInProduction/Cost/TokenCostCalculator.cs) |
| Eval cases + deterministic scorers | [`Eval/Scorers.cs`](src/AiInProduction/Eval/Scorers.cs) |
| Running an eval suite: correctness, latency, tokens, cost | [`Eval/EvalRunner.cs`](src/AiInProduction/Eval/EvalRunner.cs) |
| Tracing every AI call with `UseOpenTelemetry()` | [`Program.cs`](src/AiInProduction/Program.cs) |
| Trace + meter providers with a console exporter | [`Program.cs`](src/AiInProduction/Program.cs) |

---

## Prerequisites (for the live part)

**[Ollama](https://ollama.com)** running locally with a chat model:

```bash
ollama pull llama3.2
```

> The **cost demo and the scorer demo run without Ollama**, so you see those lessons right away.

---

## Run it

```bash
cd "12 - AI App in Production"
dotnet run --project src/AiInProduction
```

You'll see:

1. **Token cost estimation.** The same request (1,500 in / 400 out tokens) priced across several models.
2. **Scorer demo.** How a deterministic check decides PASS/FAIL.
3. **Live eval run** (if Ollama is up): 3 cases scored, with latency, tokens, and cost per case, plus the **OpenTelemetry spans** printed to the console for each call.

```
Case            Result  Latency   In/Out      Cost        Detail
arithmetic      PASS      412ms  38/2        $0.000000   matched: 4
dotnet-cli      PASS      655ms  41/9        $0.000000   matched: build
csharp-records  PASS      390ms  45/2        $0.000000   matched: record

Pass rate : 100% (3/3)
```

| Variable | Default | Meaning |
|---|---|---|
| `OLLAMA_URL` | `http://127.0.0.1:11434` | Ollama server address |
| `OLLAMA_MODEL` | `llama3.2` | chat model name |

---

## The three ideas that matter

**Evals.** You can't improve what you don't measure. An eval is a fixed set of inputs plus a way to score the outputs. When you change a prompt or a model, you rerun the suite and *know* whether it got better or worse. Start with cheap, deterministic scorers (plain C#), and add LLM-as-judge only for the fuzzy stuff.

**Token costs.** Hosted models bill per token, and input and output are priced differently. A chatbot that resends a long history every turn gets expensive quietly. Local models cost $0 per token, but you still track tokens for latency and context limits. The prices in `TokenCostCalculator` are **illustrative**. Replace them with your provider's current rates.

**Observability.** `UseOpenTelemetry()` wraps the `IChatClient`, so every call emits a span with the model, token counts, and duration. Here it's exported to the console. In production you'd send it to the Aspire dashboard, Jaeger, Grafana, or Azure Monitor.

> 🔒 **Security note:** this demo sets `EnableSensitiveData = true`, so **full prompts and responses go into your traces**. Great for learning, dangerous in production: user data and PII end up in your telemetry backend. Turn it off (or redact) before you ship.

---

## 🤖 Try these prompts with your AI assistant

**Make the scorers honest:**
> "`KeywordMatch` passes '14' for an expected '4'. Replace it with scorers that do exact match, regex, and JSON-schema checks, and show me a failing case for each."

**Export to a real dashboard:**
> "Export these traces and metrics via OTLP to the .NET Aspire dashboard running in Docker. List the exact packages, code changes, and the docker command."

**Evals in CI:**
> "Turn this eval suite into an xUnit test that fails the build if the pass rate drops below 90% or the average cost per case goes above a budget."

**Practise the review skill:**
> "Review this project as if it were going to production tomorrow. What's missing for cost control, privacy, and reliability? Give me a findings list before any code."

> 💡 **The mindset:** ask for a *plan before edits*, give *outcomes + constraints*, and don't ship an AI feature you can't measure.

---

## Next

That's all **12 projects** 🎉 Now take one of your AI apps (07, 08, or 09), add what you learned here, and actually run it. See the [build guide](BUILD-GUIDE.md) for how.
