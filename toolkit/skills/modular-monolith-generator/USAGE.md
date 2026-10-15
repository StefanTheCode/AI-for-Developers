# How to use - Modular Monolith Generator

**What it is.** Sets up or tightens a modular monolith in your stack - modules with internal-by-default code, small public contracts, in-process messaging, per-module data ownership, and a guardrail check that stops modules reaching into each other. Uses the native mechanism per stack: `internal` + `.Public` projects (.NET), Spring Modulith (Java/Kotlin), NestJS modules / `index.ts` APIs / Nx (TypeScript), packages + import-linter (Python), `internal/` packages (Go).

**When to reach for it.** Structuring a new app, auditing module boundaries, or splitting a tangled monolith into modules.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Structure this as a modular monolith: Orders, Billing, Catalog." (.NET)
- "Audit my module boundaries and tell me what leaks."
- "Add a NetArchTest so modules can't reference each other's internals."
- "Set up Spring Modulith and verify the module structure in a test."
- "Split this NestJS app into modules that only export a facade."
- "import-linter contracts so my Django apps stay independent."
- "Lay out my Go service as modules with internal/ packages."

**Get the best out of it.** Ask for the guardrail - boundaries that aren't enforced erode. Mention where data is shared today; shared tables are usually the biggest leak. Keep the shared kernel tiny.

**What you get.** The detected structure (or a proposed split), a tree showing public vs internal per module, wiring/contract/event-bus code, the guardrail test or config, and a list of existing leaks with fixes.

**Won't do.** It won't split into microservices - that's the modernization agents' job (`legacy-modernization-assistant`).
