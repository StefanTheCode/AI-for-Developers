using System.ComponentModel;
using ModelContextProtocol.Server;
using NotesMcp.Services;

namespace NotesMcp.Tools;

/// <summary>
/// The MCP tools — the functions an AI client (Claude, GitHub Copilot, Cursor)
/// can call. This is how you "expose your API" to AI.
///
/// LESSON — good tool metadata IS the API contract for the model.
/// * <see cref="McpServerToolAttribute"/> registers a method as a callable tool.
/// * <see cref="DescriptionAttribute"/> on the method and every parameter tells
///   the model WHEN and HOW to use it. Vague descriptions → the model misuses the
///   tool. Treat these like docs written for a very literal junior developer.
///
/// Dependencies (here, <see cref="NoteStore"/>) are injected by DI just like in a
/// controller — MCP tool methods are first-class citizens of the container.
/// </summary>
[McpServerToolType]
public sealed class NoteTools(NoteStore store)
{
    [McpServerTool(Name = "add_note")]
    [Description("Create a new note with a title and content. Returns the new note's id.")]
    public string AddNote(
        [Description("Short title for the note")] string title,
        [Description("The full text/body of the note")] string content)
    {
        var note = store.Add(title, content);
        return $"Created note #{note.Id}: \"{note.Title}\"";
    }

    [McpServerTool(Name = "list_notes")]
    [Description("List all saved notes, newest first, with their ids and titles.")]
    public string ListNotes()
    {
        var notes = store.GetAll();
        if (notes.Count == 0)
            return "No notes yet.";

        return string.Join("\n", notes.Select(n => $"#{n.Id}  {n.Title}  ({n.CreatedAt:yyyy-MM-dd HH:mm})"));
    }

    [McpServerTool(Name = "get_note")]
    [Description("Get the full content of a single note by its id.")]
    public string GetNote(
        [Description("The id of the note to fetch")] int id)
    {
        var note = store.Get(id);
        return note is null
            ? $"No note with id {id}."
            : $"#{note.Id} {note.Title}\n\n{note.Content}";
    }

    [McpServerTool(Name = "search_notes")]
    [Description("Search notes whose title or content contains the given text (case-insensitive).")]
    public string SearchNotes(
        [Description("Text to search for")] string query)
    {
        var matches = store.Search(query);
        if (matches.Count == 0)
            return $"No notes matched '{query}'.";

        return string.Join("\n", matches.Select(n => $"#{n.Id}  {n.Title}"));
    }

    [McpServerTool(Name = "delete_note")]
    [Description("Delete a note by its id. Returns whether a note was removed.")]
    public string DeleteNote(
        [Description("The id of the note to delete")] int id)
    {
        return store.Delete(id)
            ? $"Deleted note #{id}."
            : $"No note with id {id}.";
    }
}
