# How to use - Architecture Reviewer (agent)

**What it is.** An agent (not a single-shot skill) that reads a whole repository in any stack - **.NET, Node.js/TypeScript, Python, Java/Kotlin (Spring), Go** - detects the stack and the intended architecture style, maps the dependency graph between projects, packages, or modules, and produces a ranked report: layering and dependency-rule violations, module boundaries, coupling, CQRS hygiene, persistence boundary leaks, testability. Each finding comes with a concrete fix, plus a prioritized action list.

**When to reach for it.** A structural second opinion on a codebase, onboarding to an inherited project, an "is my architecture clean" check, or a periodic health review.

**How to use it.** Point it at a repo or folder. Because it's an agent, it explores files on its own - you don't need to paste code. Example prompts:
- "Review the architecture of this solution - is it really Clean Architecture?"
- "I inherited this NestJS monorepo - map the module boundaries and tell me what's wrong."
- "Our Django project feels tangled. Which apps are too coupled?"
- "Is this Spring Boot modular monolith keeping its modules separate?"
- "Review the package structure of this Go service."

**How to get the best out of it.**
- **Give it the whole codebase**, not one file - its value is seeing the dependency graph across units.
- **Tell it the intended style** if you know it ("this is meant to be Vertical Slice") so it reviews against the right rules; otherwise it infers.
- **Ask for the prioritized action list** to be the focus if you want a get-started-now plan rather than a full audit.
- **Lock in the result** with `architecture-test-generator` (NetArchTest/ArchUnitNET, ArchUnit, import-linter, dependency-cruiser) so the structure stops drifting.
- **Run it periodically** and diff the reports to track whether the structure is improving or rotting.

**What it won't do.** It reviews structure, not runtime behavior or business correctness. It's deliberately practical - it'll tell you when a "violation" is actually the right call for your team size, and it won't force Java-style layers onto an idiomatic Go or Python codebase.
