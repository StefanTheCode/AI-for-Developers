namespace ExpenseTracker.Models;

/// <summary>
/// A fixed set of spending categories.
///
/// LESSON — model the domain, don't stringly-type it.
/// AI assistants love to represent things like this as a plain `string`
/// ("food", "Food", "FOOD ", "grocery"...). That looks fine in a demo and
/// then explodes the moment you try to GROUP BY category, because every typo
/// becomes a new "category". An enum makes the valid values explicit, gives
/// you IntelliSense, and makes grouping/reporting reliable.
/// </summary>
public enum Category
{
    Food,
    Transport,
    Housing,
    Entertainment,
    Health,
    Utilities,
    Other
}
