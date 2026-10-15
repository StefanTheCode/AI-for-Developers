---
name: dependency-vuln-scanner
description: Scan a project's dependencies in any ecosystem for known vulnerabilities (CVEs/GHSA), deprecated or abandoned packages, and transitive risks, then recommend the minimal safe upgrades. Covers NuGet (dotnet list package --vulnerable, NuGetAudit), npm/pnpm/yarn audit, pip-audit for pip/uv/poetry, OWASP Dependency-Check for Maven/Gradle, govulncheck for Go, cargo audit, plus cross-ecosystem scanners (OSV-Scanner, Trivy, Grype, Dependabot/Renovate). Use this skill whenever the user wants to check dependencies for CVEs, vulnerable or outdated packages, transitive vulnerabilities, supply-chain risk, a Dependabot alert, or asks "are my packages safe / any vulnerable dependencies / npm audit is red / how do I fix this transitive CVE". Always use this skill for dependency-security requests; it runs the scan and prioritizes fixes.
category: Security
version: 1.0.0
---

# Dependency Vulnerability Scanner

Check dependencies (direct and transitive) for known CVEs, deprecation, and staleness, then recommend the safest upgrade path. For version drift and unused packages, use `dependency-analyzer`.

## Input - point it at your project
Works on a target: a **project/solution/repo** or **GitHub URL** (clone first: `git clone --depth 1 <url> /tmp/repo`).

## Step 1 - detect the ecosystem(s) and run the native scanner

| Manifest / lock file | Native scan | Notes |
|---|---|---|
| `*.csproj`, `packages.lock.json` | `dotnet list package --vulnerable --include-transitive` | NuGetAudit also warns during `dotnet restore` |
| `package-lock.json` / `pnpm-lock.yaml` / `yarn.lock` | `npm audit` / `pnpm audit` / `yarn npm audit` | `--omit=dev` (npm) to focus on runtime deps |
| `pyproject.toml`, `uv.lock`, `poetry.lock`, `requirements*.txt` | `pip-audit` | for uv/poetry, export a requirements file and scan it with `pip-audit -r` |
| `pom.xml`, `build.gradle(.kts)` | OWASP Dependency-Check (Maven/Gradle plugin) | needs NVD data; slow first run |
| `go.mod` | `govulncheck ./...` | reports only vulns in code paths you actually call |
| `Cargo.lock` | `cargo audit` | RustSec advisory DB |

Cross-ecosystem (one tool for a polyglot repo): **OSV-Scanner** (`osv-scanner scan -r <target>`), **Trivy** (`trivy fs <target>`), **Grype**, or Snyk. They read lock files directly, so they also work when a full restore isn't possible.

If you can't restore or reach the advisory DB (offline), inspect versions in manifests and lock files, flag known-risky ranges, and mark results "verify".

## C# / .NET details
```
dotnet restore <target>
dotnet list <target> package --vulnerable --include-transitive
dotnet list <target> package --deprecated
dotnet list <target> package --outdated
```
- NuGetAudit runs on restore (recent SDKs); set `<NuGetAuditMode>all</NuGetAuditMode>` to include transitive packages and treat `NU1903`/`NU1904` (high/critical) as errors in CI.
- Transitive fix: add a direct `PackageReference` (or a central `PackageVersion` with transitive pinning under CPM) to the patched version, or update the parent.
- Use `packages.lock.json` (`RestorePackagesWithLockFile`) for reproducible, auditable restores.

## Other ecosystems - how to force a fixed transitive version
- **npm/pnpm/yarn:** update the parent first (`npm update <parent>`); otherwise `overrides` / `pnpm.overrides` / `resolutions`. `npm audit fix` is fine; `npm audit fix --force` can jump majors - review it. Many `npm audit` hits in dev-only build tooling are low real risk - say so.
- **Python:** bump the pin in `pyproject.toml` / lock (`uv lock --upgrade-package <pkg>`, `poetry update <pkg>`) or a constraints file for transitive pins.
- **Maven:** pin in `<dependencyManagement>` or upgrade the BOM (often the Spring Boot version); Dependency-Check suppressions only for verified false positives, with a comment.
- **Gradle:** `constraints { implementation("group:name:fixed") }` or upgrade the platform/BOM.
- **Go:** `go get module@fixed && go mod tidy`; govulncheck tells you whether the vulnerable symbol is reachable - unreachable findings are lower priority, not zero.
- **Cargo:** `cargo update -p <crate>`; check `cargo audit` for yanked/unmaintained warnings too.

## What it reports (universal)
1. 🔴 **Known vulnerabilities** - package, installed version, severity (CVSS), advisory ID (CVE/GHSA), and the fixed version to move to. Transitive ones include the path that pulls them in. Prioritize: critical/high + reachable/runtime + internet-facing first.
2. 🟠 **Deprecated / abandoned packages** - with the recommended replacement.
3. 🟡 **Significantly outdated** - major versions behind (security fixes you're missing even without a listed CVE), unsupported runtime/framework versions.
4. **Transitive pulls** - a safe direct package dragging in a vulnerable transitive one -> update the parent, or pin the fixed version with the ecosystem's mechanism above.
5. **Supply-chain hygiene** - no committed lock file, install scripts from untrusted packages, typosquat-looking names, unpinned registries.

## Upgrade guidance
- Prefer the **minimal version bump** that clears the advisory (lower regression risk).
- For transitive vulns, update the parent if it ships a fixed transitive; otherwise pin directly and leave a comment explaining why.
- Note breaking-change risk for major bumps and suggest the tests to run.
- No fix available: document the risk, check if the vulnerable feature is used, mitigate (config, WAF, isolate), and set a review date.

## Make it repeatable
- Run the native scan in CI and fail on high/critical (see `cicd-pipeline-generator`).
- Enable **Dependabot** or **Renovate** for automated update PRs, and GitHub dependency review on PRs where available.
- Scan container images too (Trivy/Grype) - the base image has its own CVEs (`dockerfile-generator`).

## Output format
```
## Summary
<ecosystems, scanner(s) used, counts by severity, top 3 actions>

## Vulnerabilities
| Package | Installed | Severity | Advisory | Fixed in | Direct/Transitive (path) | Action |

## Deprecated / outdated
...

## Upgrade plan
<ordered list: minimal bumps, pins/overrides, tests to run>

## Verify on your side
<anything offline/unconfirmed, false-positive candidates>
```

## Principles
- **Transitive dependencies are your attack surface too** - always scan them.
- Patch the advisory with the smallest safe bump; don't blindly jump majors.
- Severity alone isn't priority: reachability, runtime vs dev, and exposure matter.
- Make it automatic - a one-off scan is stale the day after.

## How to use it & best prompts
"Scan my project for vulnerable packages", "any CVEs in my dependencies, including transitive", "npm audit shows 40 issues - which matter", "fix this Dependabot alert", "run govulncheck and explain the results". Pairs with `dependency-analyzer` and the `api-security-auditor` agent.
