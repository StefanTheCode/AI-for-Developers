using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

/// <summary>
/// Owns the in-memory list of expenses and persists it to a JSON file.
///
/// LESSON — collections + a single source of truth.
/// The whole app reads and writes through this one store. Nothing else is
/// allowed to hold its own copy of the list. That keeps state predictable:
/// there is exactly ONE place where an expense is created, deleted, or saved.
/// </summary>
public sealed class ExpenseStore
{
    // Encapsulation: the backing list is PRIVATE. Callers get a read-only view
    // (see GetAll) so nobody can add/remove behind the store's back and skip
    // the auto-save + id logic.
    private readonly List<Expense> _expenses = [];
    private readonly string _filePath;

    // Serializer options are created once and reused (creating them per call is
    // wasteful). Web defaults give camelCase JSON; the enum converter writes
    // "Food" instead of the number 0, so the file stays human-readable.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ExpenseStore(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    /// <summary>Read-only snapshot — callers can query but not mutate the list.</summary>
    public IReadOnlyList<Expense> GetAll() => _expenses;

    /// <summary>Adds a new expense, assigns the next id, and saves.</summary>
    public Expense Add(string description, decimal amount, Category category, DateOnly date)
    {
        // Next id = current max + 1. `DefaultIfEmpty(0)` handles the very first
        // insert (Max on an empty sequence throws — a classic AI-generated bug).
        var nextId = _expenses.Select(e => e.Id).DefaultIfEmpty(0).Max() + 1;

        var expense = new Expense(nextId, description.Trim(), amount, category, date);
        _expenses.Add(expense);
        Save();
        return expense;
    }

    /// <summary>Removes an expense by id. Returns true if something was removed.</summary>
    public bool Remove(int id)
    {
        var removed = _expenses.RemoveAll(e => e.Id == id) > 0;
        if (removed)
            Save();
        return removed;
    }

    // --- persistence -------------------------------------------------------

    private void Load()
    {
        if (!File.Exists(_filePath))
            return;

        var json = File.ReadAllText(_filePath);
        var loaded = JsonSerializer.Deserialize<List<Expense>>(json, JsonOptions);
        if (loaded is not null)
        {
            _expenses.Clear();
            _expenses.AddRange(loaded);
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_expenses, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    /// <summary>
    /// Seeds a few sample expenses so the app has something to show on first run.
    /// Only runs when the store is empty.
    /// </summary>
    public void SeedIfEmpty()
    {
        if (_expenses.Count > 0)
            return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        Add("Groceries", 54.20m, Category.Food, today.AddDays(-2));
        Add("Bus pass", 30.00m, Category.Transport, today.AddDays(-10));
        Add("Rent", 800.00m, Category.Housing, today.AddDays(-15));
        Add("Cinema tickets", 24.00m, Category.Entertainment, today.AddDays(-5));
        Add("Pharmacy", 12.75m, Category.Health, today.AddDays(-1));
        Add("Electricity bill", 65.40m, Category.Utilities, today.AddDays(-8));
        Add("Restaurant dinner", 48.90m, Category.Food, today.AddDays(-3));
    }
}
