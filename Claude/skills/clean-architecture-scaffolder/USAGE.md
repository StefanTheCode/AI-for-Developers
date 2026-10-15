# How to use - Clean Architecture Scaffolder

**What it is.** A skill that scaffolds a backend project in either **Clean Architecture** (layered: domain / application / infrastructure / API) or **Vertical Slice Architecture** (feature-first), in your stack: **.NET** solutions, **TypeScript** (NestJS or Express), **Python** (FastAPI), **Java/Kotlin** (Spring Boot), or **Go**. It gives you the commands, the dependencies wired correctly, a working sample feature, and an architecture guardrail test (NetArchTest, dependency-cruiser, import-linter, ArchUnit, or a Go import check).

**When to reach for it.** Starting a new backend, restructuring a messy one, adding a new feature/slice consistently, or settling "how should I structure this".

**How to use it.** For an existing project, point it at the repo (path or GitHub URL) and it detects your stack and house style, then scaffolds a new feature that matches. For a new project, tell it the **stack**, **style** (or ask it to recommend one), **project name**, and whether you want **CQRS**. Example prompts:
- "Scaffold a Clean Architecture solution called Acme, .NET, with MediatR."
- "Set up a NestJS project with vertical slices and show me a CreateOrder feature."
- "Structure this FastAPI app into layers and add an import-linter contract."
- "How should I lay out this Go service? Then scaffold the orders package."
- "Should this CRUD app be Clean or Vertical Slice? Then scaffold it."

**How to get the best out of it.**
- **Let it recommend the style** if you're unsure - describe team size and domain complexity. It actively warns against over-engineering a CRUD app into four layers.
- **Ask for one real feature**, not just folders. The working sample is the template you'll copy for everything else.
- **Keep the guardrail test.** It fails the build when someone breaks the dependency rule - that's what stops the structure from rotting.
- **Use it incrementally** - come back to add each new feature so they stay consistent.

**What it won't do.** It won't build your whole domain - it gives the skeleton and one exemplar feature. It won't force a heavy architecture on a simple app or fight your existing conventions.
