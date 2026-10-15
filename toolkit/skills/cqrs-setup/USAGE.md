# How to use - CQRS Setup

**What it is.** Wires CQRS into your app - commands/queries, one handler per use case, and the pipeline (validation, logging, transactions) that makes CQRS worthwhile. On .NET it uses MediatR (after a license check), a source-generated mediator, or a thin custom dispatcher; on NestJS it uses `@nestjs/cqrs`; elsewhere (Express/Fastify, Python, Spring, Go) it uses plain handlers with decorators/middleware.

**When to reach for it.** Adding CQRS, introducing (or replacing) a mediator, separating reads from writes, or standardizing a request pipeline.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Add CQRS with MediatR and a validation + transaction behavior." (.NET)
- "Set up CQRS without MediatR - a thin dispatcher with decorators."
- "Wire @nestjs/cqrs for the orders module."
- "Command handlers with a unit of work in my FastAPI service."
- "Split OrderService into command and query services in Spring."
- "Go command handlers with logging and transaction decorators."

**Get the best out of it.** Let it read an existing feature and match your conventions. Ask for the pipeline pieces (validation/transaction/logging) - that's where CQRS pays off. Pair with `clean-architecture-scaffolder` for new slices and `result-pattern-scaffolder` for handler return types.

**What you get.** The detected stack and chosen dispatcher (with reasoning), full code for commands, queries, handlers and pipeline pieces, the registration/wiring, and a note on what should stay a direct call.

**Won't do.** It won't force a bus or mediator onto a tiny CRUD app - it'll tell you when a direct call is simpler.
