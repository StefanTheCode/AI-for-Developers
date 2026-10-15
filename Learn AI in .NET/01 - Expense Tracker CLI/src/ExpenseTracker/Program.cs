using System.Globalization;
using ExpenseTracker.Models;
using ExpenseTracker.Services;

// =============================================================================
// Expense Tracker CLI — Starter project #1
// Focus: LINQ + collections, and reading/fixing AI-generated code.
//
// This file uses top-level statements (no Main boilerplate) and a simple
// menu loop. Every branch is a small, readable method so you can follow the
// data flow: input -> ExpenseStore -> ExpenseAnalyzer -> formatted output.
// =============================================================================

// Money should print with the user's culture (currency symbol, separators).
var culture = CultureInfo.CurrentCulture;

// The JSON "database" lives next to the executable so runs persist.
var dataFile = Path.Combine(AppContext.BaseDirectory, "expenses.json");
var store = new ExpenseStore(dataFile);
store.SeedIfEmpty();

Console.WriteLine("💰 Expense Tracker CLI");
Console.WriteLine($"   Data file: {dataFile}");

while (true)
{
    PrintMenu();
    Console.Write("> ");
    var choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1": ListExpenses(); break;
        case "2": AddExpense(); break;
        case "3": RemoveExpense(); break;
        case "4": ShowReport(); break;
        case "5": ShowAiFixDemo(); break;
        case "0" or "q" or "quit" or "exit":
            Console.WriteLine("Bye! 👋");
            return;
        default:
            Console.WriteLine("Unknown option. Type a number from the menu.");
            break;
    }
}

// --- menu actions ------------------------------------------------------------

void PrintMenu()
{
    Console.WriteLine();
    Console.WriteLine("──────────────────────────────");
    Console.WriteLine(" 1) List expenses");
    Console.WriteLine(" 2) Add expense");
    Console.WriteLine(" 3) Remove expense");
    Console.WriteLine(" 4) Report (LINQ in action)");
    Console.WriteLine(" 5) 'Fix the AI code' demo");
    Console.WriteLine(" 0) Quit");
    Console.WriteLine("──────────────────────────────");
}

void ListExpenses()
{
    var expenses = store.GetAll();
    if (expenses.Count == 0)
    {
        Console.WriteLine("(no expenses yet)");
        return;
    }

    Console.WriteLine();
    Console.WriteLine($"{"Id",-4}{"Date",-12}{"Category",-15}{"Amount",12}  Description");
    foreach (var e in expenses.OrderByDescending(e => e.Date))
    {
        Console.WriteLine(
            $"{e.Id,-4}{e.Date,-12:yyyy-MM-dd}{e.Category,-15}{e.Amount.ToString("C", culture),12}  {e.Description}");
    }
}

void AddExpense()
{
    Console.Write("Description: ");
    var description = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(description))
    {
        Console.WriteLine("Description is required.");
        return;
    }

    Console.Write("Amount: ");
    // TryParse instead of Parse: never let bad input crash the app.
    if (!decimal.TryParse(Console.ReadLine(), NumberStyles.Currency, culture, out var amount) || amount <= 0)
    {
        Console.WriteLine("Amount must be a positive number.");
        return;
    }

    Console.WriteLine($"Category ({string.Join(", ", Enum.GetNames<Category>())}): ");
    if (!Enum.TryParse<Category>(Console.ReadLine(), ignoreCase: true, out var category))
    {
        Console.WriteLine("Unknown category — defaulting to 'Other'.");
        category = Category.Other;
    }

    var added = store.Add(description, amount, category, DateOnly.FromDateTime(DateTime.Today));
    Console.WriteLine($"✅ Added #{added.Id}: {added.Description} {added.Amount.ToString("C", culture)}");
}

void RemoveExpense()
{
    Console.Write("Id to remove: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("That's not a number.");
        return;
    }

    Console.WriteLine(store.Remove(id) ? $"🗑️  Removed #{id}." : $"No expense with id {id}.");
}

void ShowReport()
{
    // This is where the LINQ from ExpenseAnalyzer pays off.
    var analyzer = new ExpenseAnalyzer(store.GetAll());

    Console.WriteLine();
    Console.WriteLine("📊 SPENDING REPORT");
    Console.WriteLine($"Total spent      : {analyzer.Total().ToString("C", culture)}");
    Console.WriteLine($"Avg / month      : {analyzer.AverageMonthlySpend().ToString("C", culture)}");

    Console.WriteLine();
    Console.WriteLine("By category:");
    foreach (var row in analyzer.TotalsByCategory())
        Console.WriteLine($"  {row.Category,-15}{row.Total.ToString("C", culture),12}");

    Console.WriteLine();
    Console.WriteLine("Top 3 expenses:");
    foreach (var e in analyzer.TopExpenses(3))
        Console.WriteLine($"  {e.Amount.ToString("C", culture),12}  {e.Description}");
}

void ShowAiFixDemo()
{
    // See Services/AiCodeToFix.cs for the full explanation of the bugs.
    var all = store.GetAll().ToList();

    var buggy = AiCodeToFix.AiGenerated_BiggestExpenseCategory(all);
    var fixedResult = AiCodeToFix.Fixed_BiggestExpenseCategory(all);

    Console.WriteLine();
    Console.WriteLine("🧠 'Fix the AI code' demo");
    Console.WriteLine($"  AI version says the biggest category is : {buggy}");
    Console.WriteLine($"  Fixed version (by TOTAL spend) says     : {fixedResult}");
    Console.WriteLine("  Open Services/AiCodeToFix.cs to see WHY they differ.");
}
