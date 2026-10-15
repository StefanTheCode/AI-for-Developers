using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

/// <summary>
/// Turns a flat list of expenses into insights — using LINQ.
///
/// This class is the whole point of the project: it shows the LINQ operators
/// you reach for every day — <c>Sum</c>, <c>GroupBy</c>, <c>OrderBy</c>,
/// <c>Where</c>, <c>Select</c>, <c>Take</c> — applied to a real problem.
///
/// LESSON — analysis is a PURE function of the data.
/// Nothing here mutates state or touches the disk. You give it expenses, you
/// get numbers back. Pure functions like this are trivial to unit-test and
/// easy for both humans and AI to reason about.
/// </summary>
public sealed class ExpenseAnalyzer
{
    private readonly IReadOnlyList<Expense> _expenses;

    public ExpenseAnalyzer(IReadOnlyList<Expense> expenses) => _expenses = expenses;

    /// <summary>Grand total across every expense.</summary>
    public decimal Total() => _expenses.Sum(e => e.Amount);

    /// <summary>
    /// Total spent per category, biggest first.
    /// <c>GroupBy</c> buckets the expenses, then we project each bucket to a
    /// (category, total) pair and sort. This is the "SELECT ... GROUP BY" of LINQ.
    /// </summary>
    public IReadOnlyList<CategoryTotal> TotalsByCategory() =>
        _expenses
            .GroupBy(e => e.Category)
            .Select(group => new CategoryTotal(group.Key, group.Sum(e => e.Amount)))
            .OrderByDescending(ct => ct.Total)
            .ToList();

    /// <summary>The <paramref name="count"/> most expensive individual expenses.</summary>
    public IReadOnlyList<Expense> TopExpenses(int count = 3) =>
        _expenses
            .OrderByDescending(e => e.Amount)
            .Take(count)
            .ToList();

    /// <summary>
    /// Average spend per calendar month that actually has expenses.
    /// (Average of a category with no data would divide by zero — we avoid it
    /// by only counting months that exist in the data.)
    /// </summary>
    public decimal AverageMonthlySpend()
    {
        var monthlyTotals = _expenses
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .Select(group => group.Sum(e => e.Amount))
            .ToList();

        return monthlyTotals.Count == 0 ? 0m : monthlyTotals.Average();
    }
}

/// <summary>Result row for <see cref="ExpenseAnalyzer.TotalsByCategory"/>.</summary>
public record CategoryTotal(Category Category, decimal Total);
