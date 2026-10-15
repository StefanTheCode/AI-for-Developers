---
name: mutation-testing-setup
description: Set up mutation testing to measure whether tests actually catch bugs, and interpret the mutation score and surviving mutants - Stryker.NET (C#/.NET), StrykerJS (TypeScript/JavaScript), mutmut (Python), PIT/pitest (Java/Kotlin), gremlins or go-mutesting (Go), cargo-mutants (Rust). Use this skill whenever the user wants mutation testing, Stryker, PIT, mutmut, to know if their tests are any good, test quality beyond coverage, surviving mutants, or asks "are my tests actually testing anything / mutation score / Stryker setup / pitest setup". Always use this skill for mutation-testing requests; it configures the tool and explains the results.
category: Testing
version: 1.0.0
---

# Mutation Testing Setup

Measure whether your tests actually catch bugs - not just execute code - by injecting small faults (mutants) and checking that a test fails. Then turn surviving mutants into better assertions.

## Input - point it at your project

Works on a target: a **project / test project**, a **repo**, or a **GitHub URL** (clone first: `git clone --depth 1 <url> /tmp/repo`). It can also interpret a mutation report the user pastes or points to (HTML/JSON report folder).

## Step 1 - detect the stack and tool

| Manifest | Stack | Tool | Run |
|---|---|---|---|
| `*.csproj` (+ `*.Tests.csproj`) | C# / .NET | Stryker.NET | `dotnet tool install -g dotnet-stryker`, then `dotnet stryker` in the test project folder |
| `package.json` | TS / JS | StrykerJS | `npm init stryker`, then `npx stryker run` |
| `pyproject.toml`, `setup.cfg` | Python | mutmut | `pip install mutmut`, `mutmut run`, `mutmut results` |
| `pom.xml`, `build.gradle(.kts)` | Java / Kotlin | PIT (pitest) | Maven `pitest-maven` plugin / Gradle `info.solidsoft.pitest` plugin |
| `go.mod` | Go | gremlins (or go-mutesting) | `gremlins unleash` |
| `Cargo.toml` | Rust | cargo-mutants | `cargo install cargo-mutants`, `cargo mutants` |

First confirm the test suite is **green and reasonably fast** - mutation testing reruns it many times. A flaky or slow suite makes results meaningless.

## Universal setup rules

1. **Scope the mutation** to code that matters (domain logic, pricing, auth rules, parsers). Exclude migrations, generated code, DTOs, DI/config wiring.
2. **Set thresholds** so CI can fail on regressions (high / low / break), starting near the current score, not at an aspirational number.
3. **Run incrementally in CI** (changed files / since a base branch, where the tool supports it) and do full runs nightly or on demand.
4. **Map tests to code** - tools that use coverage to run only relevant tests per mutant are dramatically faster; keep that enabled.

## Per-stack configuration

### C# / .NET (Stryker.NET)
`stryker-config.json` in the test project folder:
```json
{
  "stryker-config": {
    "project": "MyApp.csproj",
    "test-projects": ["MyApp.Tests.csproj"],
    "mutate": ["**/Core/**/*.cs", "!**/Migrations/**"],
    "thresholds": { "high": 80, "low": 60, "break": 50 }
  }
}
```
Use `--since` (against a target branch) for PR runs so only changed code is mutated.

### TypeScript / JavaScript (StrykerJS)
`npm init stryker` generates `stryker.config.*` and picks the test runner plugin (`@stryker-mutator/vitest-runner`, `jest-runner`, `mocha-runner`). Set `mutate` globs (e.g. `src/**/*.ts`, excluding `*.test.ts`/`*.spec.ts`), `thresholds` (same `high`/`low`/`break` keys), and the TypeScript checker plugin to discard mutants that don't compile. Enable `incremental` to reuse previous results.

### Python (mutmut)
Configure paths to mutate and the test command in `pyproject.toml`/`setup.cfg` (check the installed mutmut version's docs - config keys changed between major versions). `mutmut run`, then `mutmut results` and `mutmut show <id>` to inspect a survivor. Keep the pytest run fast (`-x`, no slow integration tests in the mutation run).

### Java / Kotlin (PIT)
Maven: add `org.pitest:pitest-maven` and, for JUnit 5, the `pitest-junit5-plugin` dependency to the plugin; run `mvn org.pitest:pitest-maven:mutationCoverage`. Gradle: the `info.solidsoft.pitest` plugin with `junit5PluginVersion`, run `gradle pitest`. Configure `targetClasses`, `excludedClasses`, `mutationThreshold`. PIT works on bytecode, so Kotlin is supported (expect some noise from compiler-generated code; exclude it).

### Go
`gremlins unleash` from the module root (supports thresholds for CI); `go-mutesting` (avito-tech fork) is an alternative. Scope to packages with real logic; keep `go test` for those packages fast.

## Reading results

- **Mutation score** = killed mutants / (total - ignored). Higher = tests catch more injected bugs.
- **Killed** = some test failed - good.
- **Survived** = the code was changed (e.g. `>` flipped to `>=`, a statement removed, `true` -> `false`, an arithmetic operator swapped) and **no test failed** -> a real gap in your assertions.
- **No coverage** = no test even executes that code -> a coverage gap (see `test-coverage-gap-finder`).
- **Timeout** = the mutant caused an infinite loop or hang; usually counted as detected.
- **Equivalent mutant** = the change doesn't alter behavior (e.g. a mutation in a log message or a redundant check). Ignore it and say why, don't write a test for it.

For each meaningful survivor: add or strengthen an assertion so the mutation would be caught. A test that runs code without asserting its effect lets mutants survive. Boundary survivors (`>` vs `>=`) usually mean a missing boundary-value test case.

## Output format

```
## Detected
<stack, tool, test framework, current suite runtime>

## Setup
<install commands + config file scoped to the important code>

## Results (when a report is available)
Score: <x%> (killed / survived / no coverage / timeout)

## Survivors that matter
1. <file:line> <mutation> - <why no test caught it> -> <assertion or test case to add>

## Ignored
<equivalent or low-value survivors and why>
```

## Principles

- **Coverage says code ran; mutation says tests would catch a bug.** Mutation is the truer quality signal.
- Scope mutation runs to important code (it's slow) - don't mutate the whole codebase on every CI run.
- Don't chase 100% - focus on survivors in critical logic.
- Treat survivors as "missing assertion", not "add a test for coverage".

## How to use it & best prompts

"Set up Stryker mutation testing", "set up PIT for this Maven project", "run mutmut on my Python package", "are my tests actually catching bugs", "explain these surviving mutants", "improve my tests so these mutants die". Pairs with `test-coverage-gap-finder` and `unit-test-generator`.
