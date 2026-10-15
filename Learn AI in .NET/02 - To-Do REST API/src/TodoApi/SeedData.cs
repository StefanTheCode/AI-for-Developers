using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi;

/// <summary>Adds a couple of sample to-dos on first run so the API isn't empty.</summary>
public static class SeedData
{
    public static void EnsureSeeded(TodoDbContext db)
    {
        if (db.TodoItems.Any())
            return;

        db.TodoItems.AddRange(
            new TodoItem { Title = "Read the AI-in-.NET roadmap", IsDone = true },
            new TodoItem { Title = "Build the To-Do API", IsDone = false },
            new TodoItem { Title = "Try the prompts in the README", IsDone = false });

        db.SaveChanges();
    }
}
