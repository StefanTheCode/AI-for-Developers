# 01 · Expense Tracker CLI

> **Starter project** · Focus: **LINQ + collections** and **reading & fixing AI-generated code**.

A small console app that tracks expenses, saves them to a JSON file, and turns
them into a spending report using LINQ. It's deliberately tiny so you can read
**every line** — which is the whole point of building with AI: you understand
what you ship.

---

## What you'll learn

| Concept | Where to look |
|---|---|
| Modelling a domain (records + enums, right types for money/dates) | [`Models/Expense.cs`](src/ExpenseTracker/Models/Expense.cs), [`Models/Category.cs`](src/ExpenseTracker/Models/Category.cs) |
| Collections + encapsulation + JSON persistence | [`Services/ExpenseStore.cs`](src/ExpenseTracker/Services/ExpenseStore.cs) |
| **LINQ in action** — `Sum`, `GroupBy`, `OrderBy`, `Take`, `Average` | [`Services/ExpenseAnalyzer.cs`](src/ExpenseTracker/Services/ExpenseAnalyzer.cs) |
| **Reading & fixing AI code** — spot the bugs | [`Services/AiCodeToFix.cs`](src/ExpenseTracker/Services/AiCodeToFix.cs) |
| Safe console input (`TryParse`, not `Parse`) | [`Program.cs`](src/ExpenseTracker/Program.cs) |

---

## Run it

```bash
cd "01 - Expense Tracker CLI"
dotnet run --project src/ExpenseTracker
```

On first run it seeds a few sample expenses so the report has something to show.
Data is saved to `expenses.json` next to the built executable.

Menu:

```
1) List expenses
2) Add expense
3) Remove expense
4) Report (LINQ in action)
5) 'Fix the AI code' demo
0) Quit
```

---

## The "Fix the AI code" exercise

Open [`Services/AiCodeToFix.cs`](src/ExpenseTracker/Services/AiCodeToFix.cs).
It contains a method an AI assistant might hand you for *"return the category the
user spends the most on"*. It compiles and looks right — but it:

1. Returns the single largest **receipt**, not the biggest **category total**.
2. **Throws** on an empty list.
3. Confuses "max amount" with "the category".

Run menu option **5** to see the buggy answer and the fixed answer side by side,
then read the comments to understand each fix.

---

## 🤖 Try these prompts with your AI assistant

Use this project to practise **driving** the AI, not copy-pasting from it.

**Extend it (understand every line you accept):**
> "Add a `--month 2026-01` filter so the report only counts expenses in that
> month. Keep all LINQ in `ExpenseAnalyzer`, don't touch persistence, and show
> me the plan before you edit."

**Refactor with constraints:**
> "Introduce a `Budget` per category and flag categories that are over budget in
> the report. Use records, no exceptions for control flow, and add the new logic
> as a pure method I can unit-test."

**Practise the review skill:**
> "Review `ExpenseStore.Add` for concurrency and error-handling problems.
> List issues as a numbered list before suggesting any code."

**Turn a bug into a lesson:**
> "Here's a LINQ method [paste one]. Without running it, tell me every input
> that would make it throw, and rewrite it to be safe."

> 💡 **The mindset:** ask for a *plan before edits*, describe the *outcome and
> constraints* (not the steps), and never accept code you can't explain.

---

## Next

Move on to **[02 · To-Do REST API](../02%20-%20To-Do%20REST%20API)** — Minimal APIs + EF Core + SQLite.
