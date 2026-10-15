# How to use - Architecture Test Generator

**What it is.** Generates architecture tests / fitness functions that fail the build on rule violations - layer dependencies, module boundaries, cycles, naming and visibility conventions - using the right tool for your stack: NetArchTest or ArchUnitNET (.NET), ArchUnit (Java/Kotlin), dependency-cruiser or eslint-plugin-boundaries (TypeScript), import-linter (Python), depguard or go-arch-lint (Go).

**When to reach for it.** Stopping architectural drift, enforcing the dependency rule, guarding modular-monolith boundaries, or catching circular imports before they spread.

**How to use it.** Point it at your solution/repo (path or GitHub URL). Example prompts:
- "Add NetArchTest rules to enforce my Clean layers." (.NET)
- "Stop controllers from referencing the DbContext."
- "ArchUnit tests so my Spring modules can't depend on each other."
- "dependency-cruiser config: domain can't import infrastructure, no cycles."
- "import-linter contracts for the layers in this FastAPI app."
- "depguard rules so our Go domain package stays free of GORM."

**Get the best out of it.** Let it detect your architecture so rules fit (Clean vs Vertical Slice vs modules). Keep a handful of high-value rules. Ask it to run them against the current code so you see existing violations up front.

**What you get.** The detected structure, a rule table with the reason for each rule, ready-to-paste test classes or config files, and the CI command that enforces them.

**Won't do.** It won't impose dogmatic rules that don't match your chosen style, and it won't silently weaken a rule to make it pass. Pairs with `clean-architecture-scaffolder`, `modular-monolith-generator`, and the `architecture-reviewer` agent.
