namespace ExpenseTracker.Models;

/// <summary>
/// A single expense.
///
/// LESSON — use a <c>record</c> for immutable data.
/// An expense, once written down, shouldn't silently mutate. A positional
/// <c>record</c> gives us value-equality (two expenses with the same values
/// are "equal"), a readable <c>ToString()</c>, and non-destructive updates
/// via <c>with</c> — all for free, in one line.
///
/// LESSON — use the right types for the job.
/// * <see cref="decimal"/> for money (NEVER <c>double</c> — binary floating
///   point can't represent 0.10 exactly, and money bugs are the worst bugs).
/// * <see cref="DateOnly"/> for a calendar date with no time-of-day noise.
/// </summary>
public record Expense(
    int Id,
    string Description,
    decimal Amount,
    Category Category,
    DateOnly Date);
