# 02 · To-Do REST API — Build Guide

> **Level:** Starter · **Time:** 3–5 hours · **You'll need:** .NET 10 SDK, an AI assistant, and VS Code / Rider / Visual Studio for `.http` files
>
> Part of the **[October Build Challenge](../../CHALLENGE.md)**. The [README](README.md) explains the finished project. This guide is for building it yourself with AI.

---

## The goal

A full CRUD REST API for to-dos: Minimal APIs, EF Core, and SQLite in one file, with correct status codes and validation at the boundary.

**The real goal:** knowing what EF Core does with your LINQ, and why the entity is never what the client sends.

---

## Step-by-step

### Step 1 — Scaffold + packages

```bash
dotnet new web -n TodoApi -o src/TodoApi
dotnet add src/TodoApi package Microsoft.EntityFrameworkCore.Sqlite
```

After restoring, **read the NuGet warnings.** EF Core's transitive SQLite bundle has been flagged for a CVE. The finished project pins `SQLitePCLRaw.bundle_e_sqlite3` to fix it. AI will rarely tell you this.

### Step 2 — Entity vs. DTOs

> **Prompt:** "Create a `TodoItem` EF Core entity (Id, Title, IsDone, CreatedAt) and separate `CreateTodoRequest` / `UpdateTodoRequest` records with DataAnnotations. Explain why the client should never send the entity directly."

**Check yourself:** If `POST /todos` accepted `TodoItem`, what could a client set that it shouldn't?

### Step 3 — DbContext + registration

> **Prompt:** "Create a `TodoDbContext` with a `DbSet<TodoItem>`. Configure Title as required with max length 200 in `OnModelCreating`. Register it with SQLite and create the DB on startup using `EnsureCreated`. Tell me what `EnsureCreated` can't do that migrations can."

**Check yourself:** What DI lifetime does `AddDbContext` use, and why is that right for a web API?

### Step 4 — Endpoints with a route group

> **Prompt:** "Map GET all, GET by id, POST, PUT, DELETE under `app.MapGroup("/todos")`. Use async EF methods only. Return 201 + Location on create, 204 on update/delete, 404 when missing, 400 on invalid input. Show me a table of endpoint → status codes before writing code."

### Step 5 — Validation at the boundary

Write a small `RequestValidator` on top of `System.ComponentModel.DataAnnotations.Validator` that returns `Results.ValidationProblem(...)`.

### Step 6 — Test with a `.http` file

Write one request per endpoint, including an **invalid** one (empty title) and one for a **missing** id.

---

## ⚠️ Where AI usually gets it wrong here

| AI tends to... | Why it's wrong | What to do instead |
|---|---|---|
| Bind the entity in `POST`/`PUT` | Over-posting: the client sets `Id`, `CreatedAt`, and more | DTOs + manual mapping |
| Return `200 OK` for everything | Clients and caches depend on 201/204/404 | Status code table first |
| Use `.ToList()` / `SaveChanges()` (sync) | Blocks threads under load | `ToListAsync` / `SaveChangesAsync` |
| `OrderByDescending(t => t.CreatedAt)` on SQLite | SQLite can't ORDER BY a `DateTimeOffset` in SQL, so it throws at runtime | Order by key, or store UTC ticks (see the comment in `Program.cs`) |
| Suggest `EnsureCreated` *and* migrations together | They don't mix: `EnsureCreated` skips the migrations history | Pick one: demo = EnsureCreated, real app = migrations |
| Ignore NuGet vulnerability warnings | You ship a known CVE | Read `dotnet list package --vulnerable` |

---

## 🔨 Break it on purpose

1. `POST /todos` with `{ "id": 999, "title": "hack", "createdAt": "2000-01-01" }`. Which fields did the API ignore, and **why**?
2. Change the list endpoint to order by `CreatedAt` and call it. Read the exception message all the way through.
3. Run `dotnet list package --include-transitive --vulnerable` before and after removing the SQLite pin.

---

## ✅ Definition of done

- [ ] All 5 endpoints return the correct status codes (tested in the `.http` file)
- [ ] An invalid title returns a 400 with a validation problem body
- [ ] Over-posting `id`/`createdAt` has no effect
- [ ] No vulnerable packages in `dotnet list package --vulnerable`
- [ ] You can explain what SQL EF Core generated for `FindAsync(id)` (turn on EF logging and look)
- [ ] You wrote down **one thing the AI got wrong**

## 🚀 Stretch goal

Add pagination (`page`, `pageSize`, max 100, total count in a header) and switch from `EnsureCreated` to migrations.

---

**Doing the [October Build Challenge](../../CHALLENGE.md)?** Share your repo and the one thing AI got wrong in **[AI for Developers](https://www.skool.com/ai-for-dotnet-developers/about)**. That's also where the Advanced & Expert levels come with walkthroughs, roadmaps, and someone to ask when you get stuck.
