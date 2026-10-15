<!--
HOW TO USE THIS FILE
--------------------
1. Copy everything BELOW the line into a file named `CLAUDE.md` in the root of your repository.
2. Fill in every [PLACEHOLDER]. Fastest way: ask Claude Code
   "Fill in this CLAUDE.md by scanning the repo - detect the stack, commands, and conventions. Ask me about anything you can't infer."
3. Delete sections and rules that don't apply. Short and true beats long and generic.
4. Keep it current: when Claude makes the same mistake twice, add a rule here.
Filled examples: dotnet-CLAUDE-md-template.md, typescript-CLAUDE-md-template.md, python-CLAUDE-md-template.md.
Made by Stefan Đokić · TheCodeMan · free to use and adapt.
==================================================================
-->

# Project: [PROJECT NAME]

[One or two sentences: what this system does and who uses it.]

Instructions for Claude Code working in this repository. Follow these by default; ask before deviating.

## Stack

- **Language / runtime:** [e.g. C# on .NET 10 | TypeScript on Node 22 | Python 3.12 | Java 21 | Go 1.23]
- **Framework:** [ASP.NET Core | NestJS | Express | FastAPI | Django | Spring Boot | net/http + chi]
- **Data:** [ORM / query library] with [PostgreSQL | SQL Server | MySQL | MongoDB]
- **Testing:** [test framework] + [integration test approach, e.g. Testcontainers]
- **Package manager / build:** [dotnet | pnpm | npm | uv | poetry | Maven | Gradle | go modules]
- **Architecture:** [Vertical Slice | Clean Architecture | Modular Monolith | layered MVC]
- **Other:** [messaging, cache, observability, cloud services]

## Commands

Run these exactly. Don't guess alternatives.

- Install: `[command]`
- Build: `[command]`
- Test (all): `[command]`
- Test (single file/test): `[command]`
- Lint: `[command]`
- Format: `[command]`
- Type check: `[command, if separate from build]`
- Run locally: `[command]`
- DB migrations: `[create command]` / `[apply command]`

## Project structure

```
[top-level tree, 1 line of purpose per folder]
src/
  [feature or layer folders]
tests/
```

- New features go in: [where and how, e.g. `src/features/<feature>/`].
- Match an existing feature ([path to a good example]) before writing new code. Don't introduce a new pattern without asking.

## Coding conventions

- **Errors:** [e.g. Result type for expected failures, exceptions only for the unexpected; how errors map to HTTP responses].
- **Validation:** [library and where it runs].
- **Data access:** [e.g. project to DTOs, no lazy loading in loops, always paginate lists, no raw SQL without parameters].
- **Async / concurrency:** [e.g. pass cancellation/context through every call; never block on async; no fire-and-forget].
- **Config & secrets:** [how config is read; secrets never in source].
- **Naming & style:** [file naming, casing, import order, anything the linter doesn't enforce].
- **Dependencies:** ask before adding a new package; prefer what's already in the project.

## Testing

- [Which kinds of tests are expected for which code: unit for pure logic, integration for DB/HTTP.]
- [Test naming convention and location.]
- [What not to do, e.g. no in-memory DB fakes, no mocking the ORM.]
- When fixing a bug, first write a failing test that reproduces it.

## Never do

- [Commit secrets, credentials, or `.env` files.]
- [Edit generated files or applied migrations: list paths.]
- [Disable lint rules, type checks, or tests to make the build pass.]
- [Patterns this team rejected, e.g. repository wrappers over the ORM, exceptions for control flow, service locator.]
- [Run destructive commands (drop DB, force push, delete branches) without asking.]

## Definition of done

A change is done when:
- [ ] It builds with no new warnings and the type check passes.
- [ ] Lint and format pass.
- [ ] Tests for the new behavior exist and the full test suite passes.
- [ ] [Migrations created and reviewed, if the schema changed.]
- [ ] [Docs / API contract / changelog updated, if behavior changed.]
- [ ] No TODOs, debug logging, or commented-out code left behind.

## Workflow with Claude

- For non-trivial changes, give a short **plan first** and wait for approval before editing.
- **Don't invent problems** - leave code that's already correct alone.
- Explain each change in one line (what + why).
- When unsure about a convention, read 2-3 existing features and match them.
