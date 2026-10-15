# How to use - Code Reviewer (agent)

**What it is.** An agent that does a PR-level review of changes in any language - **C#/.NET, TypeScript/JavaScript, Python, Java/Kotlin, Go** and others - covering correctness, async/concurrency, data-access and performance pitfalls, error handling, API design, security basics, readability, and test coverage. It reviews in the language's own idiom and returns severity-ranked comments (🔴 must-fix / 🟡 suggestion / 🟢 nit), a verdict (approve / approve-with-comments / request-changes), and what's done well.

**When to reach for it.** Before merging a branch, a second pair of eyes on a diff, a quality gate, or reviewing a contribution.

**How to use it.** Point it at a branch, a diff, or specific files. It can run `git diff`/`git log` to find what changed. Example prompts:
- "Review my changes on this branch before I merge."
- "Code-review this diff: \<paste or point to files\>"
- "Quality check on OrderService.cs and its tests."
- "Review this Express route and its Vitest tests."
- "Look over my FastAPI PR - anything blocking?"
- "Review the Go handler changes in this branch."

**How to get the best out of it.**
- **Scope it to the change.** Give it the branch/diff, not the whole repo, so it reviews what you're actually shipping (it'll use git to find the delta).
- **Tell it the goal of the PR** - it reviews intent-first and won't critique a deliberate trade-off without acknowledging it.
- **Use the severity split** - ship past the 🟢 nits, fix the 🔴s. Nits stay optional so they don't block you.
- **Encode your team's rules** - tell it your conventions and it'll review against them, not just generic defaults. It also reads your linter config so it doesn't repeat what tooling already enforces.

**What it won't do.** It reviews the code in front of it, not the runtime or full business correctness. It won't rubber-stamp - but it also won't nitpick formatting a linter should handle. Every review includes what's good, so it's usable feedback, not a wall of red.
