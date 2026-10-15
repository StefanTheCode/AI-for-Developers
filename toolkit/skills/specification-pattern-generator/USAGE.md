# How to use - Specification Pattern Generator

**What it is.** Implements the Specification pattern - reusable, composable, testable query criteria - in your ORM's idiom, so you stop duplicating filter logic across repositories and services: a `Specification<T>` + evaluator (or Ardalis.Specification) for EF Core, Spring Data JPA `Specification`, Prisma where-input builders / Drizzle expressions / TypeORM query-builder functions, SQLAlchemy expressions or Django custom QuerySets, and GORM scopes.

**When to reach for it.** The same filter/include/order logic is repeated across the codebase, you need composable search filters, or you want query criteria you can unit-test.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Add the specification pattern to this EF Core project." (.NET)
- "Dedupe these repeated EF queries into specs."
- "Spring Data Specifications for the order search endpoint."
- "Reusable, composable Prisma filters for orders."
- "Replace repeated Django filters with custom QuerySet methods."
- "GORM scopes for the Where clauses repeated across our Go repositories."

**Get the best out of it.** Let it scan for repeated query logic so the specs it creates target real duplication. Keep read specs read-only/no-tracking by default.

**What you get.** The detected ORM and the mechanism chosen (native vs custom), a table of duplicated criteria with where they occur, the spec code and refactored call sites, and tests per spec.

**Won't do.** It won't wrap one-off queries in ceremony - it'll tell you when a plain query is simpler. Pairs with `orm-query-optimizer`.
