---
name: codebase-health-dashboard
description: Run the whole toolkit against a repo in any stack (.NET, Node.js/TypeScript, Python, Java/Kotlin, Go, Rust, or a polyglot monorepo) and produce a single scored "health dashboard" - an overall score plus per-category scores (security, architecture, database/ORM, performance, observability, testing, dependencies, code quality), each with findings and the top fixes. It detects the stack from manifests and collects hard signals with the stack's own tools (build, tests, coverage, outdated dependencies, known vulnerabilities). Use this skill whenever the user wants a codebase health check, an overall code quality score, a full audit of their project, "how healthy is my codebase", a one-page report card, or to track their codebase score over time. Always use this skill for whole-repo health/scorecard requests; it orchestrates the auditor tools, scores them with a fixed rubric, and writes a self-contained dashboard.
category: AI Tooling
version: 1.0.0
---

# Codebase Health Dashboard

Point this at a repo and get back a **single scored report card**: one overall health score, eight category scores, the findings behind each score, and the highest-leverage fixes. It is the "run everything and tell me where I stand" tool, for any stack.

## Input - point it at your project
Works on a target: a **repo/project folder** or a **GitHub URL** (`git clone --depth 1 <url> /tmp/repo` then scan).

## Step 1 - detect the stack(s)

Find every manifest (a monorepo can have several - score the repo as a whole, run commands per project root):
```
find <target> \( -name "*.sln" -o -name "*.slnx" -o -name "*.csproj" -o -name package.json -o -name pyproject.toml \
  -o -name "requirements*.txt" -o -name pom.xml -o -name "build.gradle*" -o -name go.mod -o -name Cargo.toml \) \
  -not -path "*/node_modules/*" -not -path "*/bin/*" -not -path "*/obj/*" -not -path "*/.venv/*" -not -path "*/target/*"
```

| Manifest | Stack | Also check |
|---|---|---|
| `*.sln`/`*.slnx`, `*.csproj` | .NET | `Directory.Packages.props`, `global.json`, target framework |
| `package.json` (+ lockfile) | Node / TS | package manager from lockfile: `package-lock.json` npm, `pnpm-lock.yaml` pnpm, `yarn.lock` yarn, `bun.lock(b)` bun; `tsconfig.json` |
| `pyproject.toml`, `requirements*.txt` | Python | `uv.lock`, `poetry.lock`, Python version |
| `pom.xml`, `build.gradle(.kts)` | Java / Kotlin | wrapper (`mvnw`, `gradlew`), Spring Boot version |
| `go.mod` | Go | Go version directive |
| `Cargo.toml` | Rust | workspace members |

## Step 2 - collect hard signals (per stack)

Run what the environment supports. If a tool isn't installed, a restore needs private feeds, or a command fails for environmental reasons, record the signal as **"not measured"** - never guess a result and never penalize for an environment problem.

| Signal | .NET | Node / TS | Python | Java / Kotlin | Go | Rust |
|---|---|---|---|---|---|---|
| Build | `dotnet build` | `npm ci && npm run build --if-present` (or pnpm/yarn) + `npx tsc --noEmit` | install (`uv sync` / `pip install -e .`), `ruff check`, `mypy` if configured | `./mvnw -B verify -DskipTests` / `./gradlew build -x test` | `go build ./... && go vet ./...` | `cargo build` + `cargo clippy` |
| Tests | `dotnet test` | `npm test` (Vitest / Jest) | `pytest` | `./mvnw -B test` / `./gradlew test` | `go test ./...` | `cargo test` |
| Coverage | `dotnet test --collect:"XPlat Code Coverage"` | `vitest run --coverage` / `jest --coverage` | `pytest --cov` | JaCoCo report | `go test -coverprofile=cover.out ./... && go tool cover -func=cover.out` | `cargo llvm-cov` / `cargo tarpaulin` |
| Outdated deps | `dotnet list package --outdated` | `npm outdated` | `pip list --outdated` / `uv pip list --outdated` | `versions:display-dependency-updates` (Maven) / Gradle Versions plugin `dependencyUpdates` | `go list -u -m all` | `cargo outdated` |
| Known vulns | `dotnet list package --vulnerable --include-transitive` | `npm audit` / `pnpm audit` | `pip-audit` | OWASP Dependency-Check | `govulncheck ./...` | `cargo audit` |

Cross-stack fallbacks for vulnerabilities: `osv-scanner` or `trivy fs` over the repo. Don't run tests that need external infrastructure you can't provide - note them as not measured.

### Signal -> finding mapping (deterministic)
- Build fails: **critical** (Code Quality). New compiler/type-check/lint errors: **high**; a large warning count: **low**.
- Tests fail: **high** (Testing), one finding per failing suite/project, not per test. No tests at all: **high**.
- Line coverage of core code: < 40% **high**, 40-59% **medium**, 60-79% **low**, >= 80% no finding (Testing).
- Known vulnerability: one finding per affected package at the advisory's severity (critical/high/medium/low) (Dependencies).
- Outdated: a dependency 2+ major versions behind, or an end-of-life runtime/framework: **medium**; 1 major behind: **low**. Group minor/patch drift into one **low** finding.

## Step 3 - run the relevant auditors

Collect findings from the toolkit's auditors. Use the installed tools if present, otherwise apply their checklists directly:
- **Security** - `api-security-auditor`, `secrets-config-auditor`
- **Architecture** - `architecture-reviewer`
- **Database / ORM** - `db-performance-auditor`, `orm-query-optimizer`, `orm-config-auditor`
- **Performance** - `async-await-auditor`, `memory-allocation-analyzer`
- **Observability** - `observability-gap-finder`
- **Testing** - `test-coverage-gap-finder` + the test/coverage signals above
- **Dependencies** - `dependency-vuln-scanner`, `dependency-analyzer` + the outdated/vuln signals above
- **Code Quality** - `code-reviewer` + the build/lint signals above

Skip a category only when it genuinely has no surface (e.g. no database) and say so in the summary.

## Step 4 - score, render, record

1. **Score each category** with the fixed rubric below (deterministic, so re-runs are comparable).
2. **Fill the template** `references/health-dashboard-template.html` - replace `/*__HEALTH_DATA__*/` with the JSON object (shape below) and write it to `health-dashboard/index.html`.
3. **Save a history record** to `health/health-<YYYY-MM-DD>.json` with the same data, so the score can be tracked over time.
4. **Tell the user the overall score and the top 3 actions** to raise it the fastest.

## Scoring rubric (deterministic)
Each category starts at **100**. Subtract per finding by severity, floor at 0:
- critical: **-25**
- high: **-12**
- medium: **-5**
- low: **-2**

If a category genuinely has no applicable surface (e.g. no messaging, so some observability checks do not apply), score what exists and note it - do not invent findings to fill it.

**Overall** = rounded average of the category scores.

**Verdict bands** (per category and overall):
- 85-100: **Strong**
- 70-84: **Healthy**
- 50-69: **Needs work**
- 0-49: **At risk**

## Output data shape (inject into the template)
```json
{
  "project": "Your repo (or anonymized name)",
  "date": "2026-06-22",
  "stacks": [".NET 9", "TypeScript (React)"],
  "overall": 73,
  "summary": "One paragraph: the headline, the biggest risk, the quickest win.",
  "signals": [
    { "name": "Build", "value": "passing" },
    { "name": "Tests", "value": "412 passed, 3 failed" },
    { "name": "Coverage", "value": "58% lines" },
    { "name": "Known vulns", "value": "1 high, 2 moderate" },
    { "name": "Outdated", "value": "not measured" }
  ],
  "categories": [
    {
      "name": "Security",
      "score": 61,
      "verdict": "Needs work",
      "findings": [
        { "severity": "critical", "title": "1 of 75 endpoints require authorization", "location": "endpoints" },
        { "severity": "high", "title": "Sensitive data logging enabled in all environments", "location": "DbContext setup" }
      ]
    }
    // ... one object per category that applies
  ],
  "topActions": [
    "Add a fallback authorization policy and audit endpoint coverage.",
    "Gate EnableSensitiveDataLogging behind IsDevelopment().",
    "Push the in-memory filtering in the orders query into SQL."
  ]
}
```
`stacks` and `signals` are optional; the template renders them when present.

## Principles
- **Deterministic scoring** so the number means something and members can watch it climb week to week.
- **Measured beats inferred** - prefer the stack's own build/test/audit output over reading code; say "not measured" when you couldn't run it.
- **Honest** - credit what is healthy; do not pad findings to lower a score artificially.
- **Actionable** - every category links its score to specific findings and the fix, and the top 3 actions are the fastest wins.
- **Private** - it runs on the target the user points at; nothing leaves their machine.

## How to use it & best prompts
"Run a health check on this repo", "give my project an overall score", "audit my whole .NET solution and show me a report card", "how healthy is this TypeScript monorepo", "score my Spring Boot service". Re-run it weekly and watch the score move. Pairs with every auditor agent and the `toolkit-dashboard`.
