using Microsoft.EntityFrameworkCore;
using TodoApi;
using TodoApi.Data;
using TodoApi.Models;

// =============================================================================
// To-Do REST API — Starter project #2
// Focus: Minimal APIs + EF Core + SQLite.
//
// This whole API — DI setup, database, and 5 REST endpoints — lives in one
// readable file. That's the Minimal API style: no controllers, no ceremony,
// just the routes and the handlers.
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

// --- Services (dependency injection) ----------------------------------------

// Register the DbContext. The SQLite database is a single file, "todos.db",
// created next to the app. Swapping to SQL Server later is a one-line change.
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlite("Data Source=todos.db"));

var app = builder.Build();

// --- Create the database on startup -----------------------------------------

// EnsureCreated() builds the schema from the model if the file doesn't exist.
// It's perfect for a demo. For a REAL app you'd use EF Core *migrations*
// (see the README) so you can evolve the schema safely over time.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    db.Database.EnsureCreated();
    SeedData.EnsureSeeded(db);
}

// --- Middleware --------------------------------------------------------------

// --- Endpoints ---------------------------------------------------------------
// A route group keeps the URL prefix ("/todos") in one place and lets us tag
// everything for OpenAPI at once.
var todos = app.MapGroup("/todos").WithTags("Todos");

// GET /todos  ->  list all (newest first)
// NOTE: we order by Id (which auto-increments, so higher = newer) instead of
// CreatedAt. SQLite can't ORDER BY a DateTimeOffset in SQL — a real provider
// quirk worth knowing. Ordering by the key sidesteps it and is just as correct here.
todos.MapGet("/", async (TodoDbContext db) =>
    await db.TodoItems
            .OrderByDescending(t => t.Id)
            .ToListAsync());

// GET /todos/{id}  ->  one item, or 404
todos.MapGet("/{id:int}", async (int id, TodoDbContext db) =>
    await db.TodoItems.FindAsync(id) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

// POST /todos  ->  create. Returns 201 + Location header pointing at the new item.
todos.MapPost("/", async (CreateTodoRequest request, TodoDbContext db) =>
{
    // Validate the incoming DTO at the boundary; bail out with 400 if invalid.
    if (RequestValidator.Validate(request) is { } validationError)
        return validationError;

    // Map the DTO to the entity ourselves — the client never sets Id/CreatedAt.
    var todo = new TodoItem { Title = request.Title };

    db.TodoItems.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todos/{todo.Id}", todo);
});

// PUT /todos/{id}  ->  update title + done state
todos.MapPut("/{id:int}", async (int id, UpdateTodoRequest request, TodoDbContext db) =>
{
    if (RequestValidator.Validate(request) is { } validationError)
        return validationError;

    var todo = await db.TodoItems.FindAsync(id);
    if (todo is null)
        return Results.NotFound();

    todo.Title = request.Title;
    todo.IsDone = request.IsDone;
    await db.SaveChangesAsync();

    return Results.NoContent();
});

// DELETE /todos/{id}
todos.MapDelete("/{id:int}", async (int id, TodoDbContext db) =>
{
    var todo = await db.TodoItems.FindAsync(id);
    if (todo is null)
        return Results.NotFound();

    db.TodoItems.Remove(todo);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

// Root redirects to the list endpoint so opening the site shows something useful.
app.MapGet("/", () => Results.Redirect("/todos"));

app.Run();
