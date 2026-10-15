using System.Collections.Concurrent;

namespace NotesMcp.Services;

/// <summary>A single note.</summary>
public sealed record Note(int Id, string Title, string Content, DateTimeOffset CreatedAt);

/// <summary>
/// A thread-safe, in-memory notes store — this is "your API" that we expose to AI.
///
/// LESSON — MCP tools call into ordinary services.
/// There's nothing AI-specific here. It's a plain service you might already have.
/// The MCP layer (see Tools/NoteTools.cs) just wraps these methods so an AI client
/// can call them. Registered as a singleton so all tool calls share one store.
/// </summary>
public sealed class NoteStore
{
    private readonly ConcurrentDictionary<int, Note> _notes = new();
    private int _nextId;

    public Note Add(string title, string content)
    {
        var id = Interlocked.Increment(ref _nextId);
        var note = new Note(id, title, content, DateTimeOffset.UtcNow);
        _notes[id] = note;
        return note;
    }

    public IReadOnlyList<Note> GetAll() =>
        _notes.Values.OrderByDescending(n => n.CreatedAt).ToList();

    public Note? Get(int id) => _notes.GetValueOrDefault(id);

    public bool Delete(int id) => _notes.TryRemove(id, out _);

    public IReadOnlyList<Note> Search(string query) =>
        _notes.Values
            .Where(n => n.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || n.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(n => n.CreatedAt)
            .ToList();
}
