---
name: test-coverage-gap-finder
description: Find the meaningful test-coverage gaps in any codebase - .NET, TypeScript/JavaScript, Python, Java/Kotlin, Go, Rust - untested modules with real logic, uncovered branches, and risky code without tests, then prioritize what to test next by risk. Works from the source tree alone or from a coverage report (Cobertura XML, lcov, JaCoCo, coverage.py, Go coverprofile). Use this skill whenever the user wants to find coverage gaps, what to test next, untested code, low-coverage areas, or asks "what's not tested", "where should I add tests", "analyze my coverage report", "set up coverage". Always use this skill for coverage-gap analysis; it prioritizes by risk, not vanity percentage.
---

# Test Coverage Gap Finder

Find where tests are actually missing and prioritize by risk - so effort goes to the code that matters, not to chasing a coverage percentage.

## Input - point it at your project / coverage report

Works on a **project** or **GitHub URL** (clone with `git clone --depth 1 <url> /tmp/repo`), optionally with an existing coverage report. Without a report, map source files to test files statically; with one, parse it for zero/low-covered files and uncovered branches.

## Step 1 - detect the stack and test conventions

| Manifest | Test frameworks | Test file convention |
|---|---|---|
| `*.csproj` | xUnit, NUnit, MSTest | `*.Tests` projects, `*Tests.cs` |
| `package.json` | Vitest, Jest, Mocha, node:test | `*.test.ts`, `*.spec.ts`, `__tests__/` |
| `pyproject.toml` / `requirements*.txt` | pytest, unittest | `tests/`, `test_*.py`, `*_test.py` |
| `pom.xml` / `build.gradle(.kts)` | JUnit 5, Kotest, TestNG | `src/test/java`, `src/test/kotlin`, `*Test.java` |
| `go.mod` | `testing` | `*_test.go` next to the code |
| `Cargo.toml` | built-in tests | `#[cfg(test)] mod tests`, `tests/` |

Quick static map (adapt globs to the stack):
```
# source files with no sibling/mirrored test file
find <target>/src -name "*.ts" ! -name "*.test.ts" ! -name "*.spec.ts" -not -path "*/node_modules/*"
find <target> -name "*.go" ! -name "*_test.go" -not -path "*/vendor/*"
grep -rln "public class\|public record" --include=*.cs <target> | grep -v "Tests\|/obj/\|/bin/"
```

## Step 2 - get real coverage (when possible)

| Stack | Generate coverage | Report format |
|---|---|---|
| .NET | `dotnet test --collect:"XPlat Code Coverage"` (coverlet collector) | `coverage.cobertura.xml`; ReportGenerator for HTML |
| TS/JS - Vitest | `npx vitest run --coverage` (`@vitest/coverage-v8` or `-istanbul`) | lcov / json / html in `coverage/` |
| TS/JS - Jest | `npx jest --coverage` | lcov in `coverage/` |
| TS/JS - other | `npx c8 <test command>` | lcov / text |
| Python | `pytest --cov=<package> --cov-branch --cov-report=xml` (pytest-cov) or `coverage run --branch -m pytest && coverage xml` | `coverage.xml` (Cobertura) |
| Java/Kotlin | JaCoCo Maven plugin (`mvn verify` with the `report` goal) or Gradle `jacoco` plugin (`./gradlew test jacocoTestReport`); Kover for Kotlin | `jacoco.xml` / HTML |
| Go | `go test ./... -coverprofile=cover.out -covermode=atomic`, then `go tool cover -func=cover.out` | coverprofile (line/statement only) |
| Rust | `cargo llvm-cov` or `cargo tarpaulin` | lcov / html |

Prefer **branch** coverage where the tool supports it - uncovered branches are where bugs hide. Go's built-in tool reports statements only; read the code for untested branches there.

## Step 3 - prioritize by risk (not vanity %)

Score each gap by **risk x complexity**:

1. 🔴 **Untested modules with real logic** - services, use-case handlers, domain rules, validators, parsers, pricing/billing calculations, state machines.
2. 🔴 **Critical paths, even if partly covered** - auth/authorization, payments, money and date math, data migrations, anything security- or compliance-relevant.
3. 🟡 **Uncovered branches in covered code** - error paths, `else`/`default` arms, early returns, retries, timeouts, null/empty inputs.
4. 🟡 **Integration seams** - DB queries, external API clients, message handlers: covered only by mocks, or not at all.
5. 🟢 **Deprioritize** - DTOs/data classes, generated code, trivial getters/setters, framework wiring, thin pass-through controllers.

Boosters: code that changed recently (`git log --since=3.months --name-only`), files with many bug-fix commits, high cyclomatic complexity.

## Per-stack notes

- **.NET** - exclude generated code and migrations (`[ExcludeFromCodeCoverage]`, coverlet `ExcludeByFile`). For DB-touching code, suggest integration tests with `WebApplicationFactory` + Testcontainers rather than mocking `DbContext` or using the in-memory provider.
- **TS/JS** - make sure untested files appear in the report (`coverage.include` in Vitest, `--all` in c8, `collectCoverageFrom` in Jest); otherwise never-imported files are invisible.
- **Python** - set `source` in `.coveragerc` / `[tool.coverage.run]` so unimported modules show as 0%; enable `branch = true`.
- **Java/Kotlin** - exclude Lombok/generated code; Spring wiring classes are low priority. Use Testcontainers for repository/integration tests.
- **Go** - use `-coverpkg=./...` when tests in one package exercise others; table-driven tests are the cheap way to cover branches.

## Output

```
## Stack and data source
<stack, test framework, coverage tool used or "static analysis only">

## Coverage gaps (ranked)
🔴 <module/type> - no tests; contains <business logic> -> suggest: <key cases>
🟡 <function> - branch `<condition>` uncovered -> add case
🟢 (skip) <DTOs / generated / trivial>

## Suggested next 5 tests
1. <test name> - <what it proves> (<unit | integration>)
```

## Principles

- **Coverage % is a vanity metric.** 100% of trivial code is worthless; 60% covering all the risky branches is great.
- Prioritize by **risk x complexity**, not line count.
- A gap in a payment or auth path outranks ten gaps in DTOs.
- Don't recommend tests for code that can't meaningfully fail.
- Recommend the right level: unit for pure logic, integration (real DB via containers) for data access.

## How to use it & best prompts

"What's not tested in this project", "where should I add tests next", "analyze my coverage report and prioritize", "set up coverage for this repo", "find untested business logic".
