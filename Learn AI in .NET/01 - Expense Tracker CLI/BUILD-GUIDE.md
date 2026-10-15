# 01 · Expense Tracker CLI — Build Guide

> **Level:** Starter · **Time:** 3–4 hours · **You'll need:** .NET 10 SDK + any AI assistant (Claude Code, Copilot, Codex)
>
> Part of the **[October Build Challenge](../../CHALLENGE.md)**. The [README](README.md) explains the finished project. This guide is for building it **yourself, from an empty folder, with AI**, and understanding every line.

---

## The goal

A console app that adds, removes, and lists expenses, saves them to JSON, and prints a spending report built with LINQ.

**The real goal:** by the end, you can explain why each type, collection, and LINQ operator is there. If you can't explain a line, you don't ship it.

---

## Rules for this build

1. **Plan before code.** Ask the AI for a plan first, then approve each step.
2. **One step at a time.** Don't ask for "the whole app". That's how you end up with 300 lines nobody understands.
3. **Answer the "Check yourself" question before you move on.** Answer it without asking the AI.

---

## Step-by-step

### Step 1 — Scaffold

```bash
dotnet new console -n ExpenseTracker -o src/ExpenseTracker
```

Make sure `<Nullable>enable</Nullable>` is on in the `.csproj`.

### Step 2 — Model the domain (before any logic)

> **Prompt:** "I'm building an expense tracker CLI in .NET 10. Before writing code, propose the types for an Expense: id, description, amount, category, date. For each field, justify the C# type you picked. Don't write the store or the UI yet."

The target is a positional `record Expense(int Id, string Description, decimal Amount, Category Category, DateOnly Date)` and an `enum Category`.

**Check yourself:** Why `decimal` and not `double`? Why an `enum` and not a `string`? Why `DateOnly` and not `DateTime`?

### Step 3 — The store (single source of truth)

> **Prompt:** "Create an `ExpenseStore` that owns a private `List<Expense>`, exposes it only as `IReadOnlyList<Expense>`, and persists to a JSON file. Add/Remove must auto-save. Enums should serialize as strings. Show the plan first."

Look for: a `static readonly JsonSerializerOptions` (created once), `JsonStringEnumConverter`, and next-id logic that works when the list is empty.

**Check yourself:** What happens to `_expenses.Max(e => e.Id)` when the list is empty? How does `DefaultIfEmpty(0)` fix it?

### Step 4 — The analyzer (LINQ, pure functions)

> **Prompt:** "Create an `ExpenseAnalyzer` that takes `IReadOnlyList<Expense>` and exposes: `Total()`, `TotalsByCategory()` (biggest first), `TopExpenses(n)`, and `AverageMonthlySpend()`. No I/O, no mutation. It has to be pure so I can unit-test it."

**Check yourself:** In `AverageMonthlySpend`, what do you divide by: 12, months in the date range, or months that have data? Which one is correct for your definition, and why?

### Step 5 — The console loop

> **Prompt:** "Write a menu loop in `Program.cs` using top-level statements. Use `TryParse` for all input and never `Parse`. Print money with the current culture's currency format."

### Step 6 — The "fix the AI code" exercise

Open a **fresh** AI chat with no context and ask:
> "Write a C# method that returns the category the user spends the most money on."

Compare the answer with [`Services/AiCodeToFix.cs`](src/ExpenseTracker/Services/AiCodeToFix.cs). Did your AI make the same mistake?

---

## ⚠️ Where AI usually gets it wrong here

| AI tends to... | Why it's wrong | What to do instead |
|---|---|---|
| Use `double` for money | `0.1 + 0.2 != 0.3` in binary floating point | `decimal`, always |
| Use `string Category` | "Food", "food", and "food " become three different groups | `enum` |
| Call `.Max()` / `.First()` with no guard | Throws `InvalidOperationException` on an empty list | `DefaultIfEmpty`, an explicit empty check, or `FirstOrDefault` |
| Answer "biggest category" with the single largest receipt | Wrong question answered, with correct-looking code | `GroupBy` → `Sum` → order |
| Use `decimal.Parse(Console.ReadLine())` | One typo crashes the app | `TryParse` + a message |
| Expose `public List<Expense>` | Anyone can mutate it and skip auto-save | Private list + `IReadOnlyList` |

---

## 🔨 Break it on purpose

1. **Corrupt the data file.** Open `expenses.json`, delete a closing bracket, and run the app. What happens? *(Hint: `Load()` has no protection. That's your first real fix.)*
2. **Change the culture.** Add `CultureInfo.CurrentCulture = new("sr-Latn-RS");` at the top and enter `12.50`, then `12,50`. Which one works?
3. **Empty everything.** Remove all expenses and run the report. Does anything throw?

---

## ✅ Definition of done

- [ ] The app runs, and the data survives a restart
- [ ] The report matches a total you calculated by hand for 3–4 expenses
- [ ] Nothing throws on an empty list
- [ ] A corrupted `expenses.json` doesn't crash the app (you fixed `Load()`)
- [ ] You can explain every LINQ operator in `ExpenseAnalyzer` without looking
- [ ] You wrote down **one thing the AI got wrong** during the build

## 🚀 Stretch goal

Add an xUnit test project for `ExpenseAnalyzer` (it's pure, so no mocks are needed), plus a `--month 2026-10` filter.

---

**Doing the [October Build Challenge](../../CHALLENGE.md)?** Share your repo and the one thing AI got wrong in **[AI for Developers](https://www.skool.com/ai-for-dotnet-developers/about)**. That's also where the Advanced & Expert levels come with walkthroughs, roadmaps, and someone to ask when you get stuck.
