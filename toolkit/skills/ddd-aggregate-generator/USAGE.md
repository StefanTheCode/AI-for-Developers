# How to use - DDD Aggregate Generator

**What it is.** Generates DDD building blocks - aggregate roots that protect invariants, value objects, and domain events - instead of anemic entities with public setters. Works in C#, TypeScript, Python, Java/Kotlin, and Go, and maps the model to your ORM (EF Core, Prisma/TypeORM, SQLAlchemy/Django, JPA/Hibernate, GORM/sqlc) without leaking it into the domain.

**When to reach for it.** Modeling a domain, turning an anemic entity rich, extracting value objects, or adding domain events.

**How to use it.** Point it at your project (path or GitHub URL) and describe the rules. Example prompts:
- "Model an Order aggregate: needs at least one line, can be confirmed only when pending." (C#)
- "Turn this anemic Customer entity into a proper aggregate."
- "TypeScript Order aggregate with a private constructor, mapped from our Prisma model."
- "Extract Money and Email value objects in my FastAPI/SQLAlchemy app."
- "Spring aggregate that registers domain events with AbstractAggregateRoot."
- "Go aggregate with unexported fields and sentinel errors."

**Get the best out of it.** State the invariants explicitly - they become guarded factory/method logic. Let it match your existing base types and Result/error conventions. Pair with `result-pattern-scaffolder` for method return types.

**What you get.** The detected stack and conventions, full code for the aggregate, value objects and events, a table of which invariant is enforced where, and persistence mapping notes.

**Won't do.** It won't over-engineer a simple CRUD table into a heavy aggregate - it'll tell you when a plain entity is enough.
