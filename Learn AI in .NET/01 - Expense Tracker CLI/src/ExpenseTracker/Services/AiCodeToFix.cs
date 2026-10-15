using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

/// <summary>
/// 🧠 LEARNING EXERCISE — "Read &amp; fix AI code".
///
/// One of the two Starter skills for this project is reading AI-generated code
/// critically and fixing it. AI assistants produce code that *looks* correct
/// and compiles, but hides subtle bugs. Below is a realistic example of what an
/// assistant might hand you, followed by the corrected version.
///
/// Read <see cref="AiGenerated_BiggestExpenseCategory"/> first and try to spot
/// the bugs yourself BEFORE reading the comments in <see cref="Fixed_BiggestExpenseCategory"/>.
/// </summary>
public static class AiCodeToFix
{
    // ------------------------------------------------------------------
    // ❌ AI-GENERATED VERSION (looks fine, has real bugs)
    //
    // Prompt that produced something like this:
    //   "Write a C# method that returns the name of the category the user
    //    spends the most money on."
    // ------------------------------------------------------------------
    public static string AiGenerated_BiggestExpenseCategory(List<Expense> expenses)
    {
        // BUG 1: `Max()` here returns the biggest *amount*, not the category.
        //        The variable name lies about what it holds.
        // BUG 2: No handling for an empty list — `Max()`/`First()` will throw
        //        an InvalidOperationException on empty input.
        // BUG 3: Uses double-style formatting for money and ignores culture.
        var biggest = expenses.Max(e => e.Amount);

        var category = expenses.First(e => e.Amount == biggest).Category;

        return category.ToString();
    }

    // ------------------------------------------------------------------
    // ✅ FIXED VERSION
    //
    // What we actually want: the category with the highest *total* spend
    // (summing all expenses in it), safe on empty input, correctly typed.
    // ------------------------------------------------------------------
    public static Category? Fixed_BiggestExpenseCategory(IReadOnlyList<Expense> expenses)
    {
        // FIX 2: guard the empty case explicitly and return null (no exception).
        if (expenses.Count == 0)
            return null;

        // FIX 1: group by category, SUM each group, then pick the biggest total.
        //        This answers the real question instead of "single largest receipt".
        return expenses
            .GroupBy(e => e.Category)
            .OrderByDescending(group => group.Sum(e => e.Amount))
            .Select(group => group.Key)
            .First();
    }
}
