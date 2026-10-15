# How to use - Mutation Testing Setup

**What it is.** Sets up mutation testing to measure whether your tests actually catch bugs (not just run code), and interprets the score and surviving mutants. It detects your stack and uses the standard tool: Stryker.NET (.NET), StrykerJS (TS/JS), mutmut (Python), PIT (Java/Kotlin), gremlins or go-mutesting (Go), cargo-mutants (Rust).

**When to reach for it.** You have tests but want to know if they're any good - quality beyond coverage %.

**How to use it.** Point it at your project or test project (path or GitHub URL), or at an existing mutation report. Example prompts:
- "Set up Stryker mutation testing for this .NET project."
- "Add StrykerJS with the Vitest runner to `packages/api`."
- "Run mutmut on my Python package and explain the survivors."
- "Configure PIT for this Spring Boot Maven build with a CI threshold."
- "Are my Go tests actually catching bugs?"

**Get the best out of it.** Make sure your suite is green and fast first. Scope mutation to important code - it's slow on a whole codebase. Use incremental/changed-files runs in CI and full runs nightly. Treat each survivor as a missing assertion.

**What you get.** Install commands and a scoped config with thresholds, then (given a report) the score, the survivors that matter with the assertion or test case to add, and the equivalent mutants to ignore.

**Won't do.** It won't chase a 100% mutation score - it focuses survivors in critical logic. Pairs with `test-coverage-gap-finder` and `unit-test-generator`.
