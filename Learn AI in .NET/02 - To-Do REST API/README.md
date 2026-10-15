# 02 · To-Do REST API

> **Starter project** · Focus: **Minimal APIs** + **EF Core** + **SQLite**.

A complete CRUD REST API for to-do items. It uses the Minimal API style (no
controllers), stores data in a single-file SQLite database via EF Core, and
validates input at the boundary. Small enough to read end-to-end.

---

## What you'll learn

| Concept | Where to look |
|---|---|
| Minimal API endpoints + route groups | [`Program.cs`](src/TodoApi/Program.cs) |
| EF Core `DbContext` + `DbSet` (LINQ → SQL) | [`Data/TodoDbContext.cs`](src/TodoApi/Data/TodoDbContext.cs) |
| Entities vs. DTOs (avoiding over-posting) | [`Models/TodoItem.cs`](src/TodoApi/Models/TodoItem.cs) |
| Input validation at the boundary | [`RequestValidator.cs`](src/TodoApi/RequestValidator.cs) |
| Correct REST status codes (200/201/204/404/400) | [`Program.cs`](src/TodoApi/Program.cs) |

---

## Run it

```bash
cd "02 - To-Do REST API"
dotnet run --project src/TodoApi
```

The API starts on `http://localhost:5080`. On first run it creates `todos.db`
and seeds a few items.

- **Try the endpoints:** open [`src/TodoApi/TodoApi.http`](src/TodoApi/TodoApi.http)
  in VS Code and click **Send Request** above each call.
- Or with curl: `curl http://localhost:5080/todos`

### Endpoints

| Method | Route | Meaning | Success code |
|---|---|---|---|
| GET | `/todos` | list all | 200 |
| GET | `/todos/{id}` | get one | 200 / 404 |
| POST | `/todos` | create | 201 + `Location` |
| PUT | `/todos/{id}` | update | 204 / 404 |
| DELETE | `/todos/{id}` | delete | 204 / 404 |

Sending an empty `title` returns **400** with a validation problem — try the
"INVALID" request in the `.http` file.

---

## About the database

This project uses `db.Database.EnsureCreated()` for simplicity — it builds the
schema from the model on startup. That's great for learning, but a real app
should use **EF Core migrations** so the schema can evolve without dropping data:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/TodoApi
dotnet ef database update --project src/TodoApi
```

> 🔒 **Security note:** the SQLite native bundle is pinned to a patched version
> in the `.csproj` because EF Core's default transitive version is flagged for a
> known CVE. Watching NuGet's vulnerability warnings is part of the job.

---

## 🤖 Try these prompts with your AI assistant

**Add a feature (with constraints):**
> "Add pagination to `GET /todos` using `page` and `pageSize` query params
> (defaults 1 and 20, max 100). Keep it in `Program.cs`, validate the params,
> and return the total count in a response header. Show the plan first."

**Introduce a proper layer:**
> "Extract the endpoint handlers into a `TodoEndpoints` static class with an
> extension method `MapTodoEndpoints(this IEndpointRouteBuilder)`. Don't change
> behaviour; just move code. Explain why this scales better than one big file."

**Practise the review skill:**
> "Review these endpoints for REST correctness and security. Am I returning the
> right status codes? Any over-posting risk? Answer as a checklist before code."

**Level up the data layer:**
> "Convert this project from `EnsureCreated` to EF Core migrations. List the exact
> commands and any code changes, and warn me about anything that could lose data."

> 💡 **The mindset:** ask for a *plan before edits*, give *outcomes + constraints*,
> and read every generated line — especially anything touching the database.

---

## Next

Move on to **[03 · Weather Dashboard](../03%20-%20Weather%20Dashboard)** — `HttpClient` + `async`/`await`.
