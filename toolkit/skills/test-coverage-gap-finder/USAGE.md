# How to use - Test Coverage Gap Finder

**What it is.** Finds meaningful test gaps (untested modules with real logic, uncovered branches, risky code) in any stack and prioritizes what to test next by risk - not by chasing a coverage percentage. It knows the coverage tool for each stack: **coverlet** (.NET), **Vitest/Jest coverage, c8/istanbul** (JS/TS), **coverage.py / pytest-cov** (Python), **JaCoCo / Kover** (Java/Kotlin), **go test -cover** (Go), **cargo llvm-cov / tarpaulin** (Rust).

**When to reach for it.** Deciding where to add tests, setting up coverage, or making sense of a coverage report.

**How to use it.** Point it at your project (path or GitHub URL), optionally with a coverage file. Example prompts:
- "What's not tested in this project?"
- "Set up coverage for this FastAPI repo and tell me what to test first."
- "Analyze my lcov report and prioritize."
- "Find untested business logic in this Spring Boot service."

**Get the best out of it.** Give it a coverage report with **branch** coverage if your tool supports it - branch gaps are where bugs hide. Tell it which areas are business-critical (payments, auth) so it weights them up.

**Won't do.** It won't push for 100% coverage - it deprioritizes DTOs, generated, and trivial code on purpose.
