<!--
HOW TO USE THIS FILE
--------------------
This is the .NET example of the generic, stack-agnostic CLAUDE-md-template.md, filled in for a .NET 10 / C# 14 API.
1. Copy the content BELOW the line into a file named `CLAUDE.md` in the root of your .NET solution.
2. Fill in the bracketed [PLACEHOLDERS] with your project's specifics (or ask Claude Code: "fill in this CLAUDE.md by scanning my solution").
3. Delete any rules that don't apply to your stack.
That's it - Claude Code now writes .NET the way you want by default.
Made by Stefan Đokić · TheCodeMan · free to use and adapt.
==================================================================
-->

# Project: [PROJECT NAME]

Instructions for Claude Code working in this repository. Follow these by default; ask before deviating.

## Stack

- **.NET 10 / C# 14** (target framework: `net10.0`)
- **Web:** ASP.NET Core Minimal APIs
- **Data:** EF Core 10 with [PostgreSQL / SQL Server]
- **Validation:** FluentValidation
- **Testing:** xUnit v3 + WebApplicationFactory + Testcontainers
- **Architecture:** [Vertical Slice / Clean Architecture / Modular Monolith]
- **Other:** [Wolverine / MassTransit, Serilog, OpenTelemetry, Aspire - list what you use]

## Architecture & structure

- Organize by **feature**, not by technical layer. Everything for one feature lives together in its feature folder (`Features/[Feature]/...`).
- Keep the request → handler → response flow explicit. No hidden magic.
- Do **not** add a repository wrapper over `DbContext` - EF Core's `DbContext` is already a Unit of Work + repository. Use it directly.
- Match the conventions in existing feature folders before writing new code. Don't introduce a new pattern without asking.

## Coding conventions (non-negotiable)

- **Errors:** use the **Result pattern** for expected failures (not-found, validation, conflict). Do **not** throw exceptions for control flow. Reserve exceptions for the genuinely unexpected. Map results to HTTP via `ProblemDetails` at the endpoint.
- **Time:** never use `DateTime.Now` / `DateTime.UtcNow` directly - inject `TimeProvider` (testable, deterministic).
- **Async:** every async method takes and passes a `CancellationToken`. Never block on async (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`). No `async void` except event handlers.
- **APIs:** use `TypedResults`, not `Results`. DTOs are immutable `record` types with `required` members where appropriate. Never expose EF entities over the wire.
- **EF Core:** project to DTOs with `.Select()`; add `.AsNoTracking()` on read queries; avoid N+1 (no lazy loading in loops); paginate list queries (`Skip`/`Take` + stable `OrderBy`). Prefer `AnyAsync()` over `CountAsync() > 0`.
- **Config:** read via the **Options pattern** (strongly-typed + `ValidateOnStart`), never raw `IConfiguration["..."]` in business logic. Secrets stay out of source.
- **Caching:** use `HybridCache` (L1+L2, tag invalidation, stampede protection), not hand-rolled `IDistributedCache` serialize/deserialize.
- **Resilience:** wrap outbound HTTP with a **Polly v8** resilience pipeline via `AddResilienceHandler` (retry + jitter, circuit breaker, timeout). No hand-rolled retry loops.
- **Modern C#:** use primary constructors, collection expressions, pattern matching, and `record` where they genuinely improve the code - not for their own sake.

## Testing

- Prefer **integration tests** for anything touching the database: `WebApplicationFactory<Program>` + **Testcontainers** (real engine). Do **not** use `UseInMemoryDatabase` or mock `DbContext` - they hide real bugs.
- Unit tests only for pure logic with no I/O.
- Test names: `Method_Scenario_ExpectedResult`. One logical assertion per test. Arrange-Act-Assert.
- When fixing a bug, first write a failing test that reproduces it, then fix.

## What to avoid

- Repository/UoW wrappers over EF Core.
- Exceptions as control flow.
- `DateTime.Now`, magic strings, service-locator (`GetService` in business logic).
- In-memory database in tests.
- Over-abstracting: no interface with a single implementation "just in case."

## Commands

- Build: `dotnet build`
- Test: `dotnet test`
- Run: `dotnet run --project [src/Api]`
- Format: `dotnet format`
- [Add your migration / docker / CI commands here]

## Workflow with Claude

- For non-trivial changes, give a short **plan first** and wait for approval before editing.
- **Don't invent problems** - leave code that's already correct and idiomatic alone.
- Explain each change in one line (what + why).
- When unsure about a convention, read 2-3 existing features and match them.
