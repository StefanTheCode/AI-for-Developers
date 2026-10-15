---
name: dependency-analyzer
description: Analyze a project's or monorepo's dependencies in any ecosystem - NuGet (incl. Central Package Management), npm/pnpm/yarn, pip/uv/poetry, Maven/Gradle, Go modules, Cargo - for version drift across projects, transitive conflicts, duplicate and unused packages, and centralization opportunities. Use this skill whenever the user wants to analyze dependencies, fix version conflicts, consolidate package versions, set up Central Package Management, pnpm catalogs, Gradle version catalogs, Maven BOMs, find unused packages, dedupe node_modules, or asks "why do I have version conflicts / clean up my NuGet or npm deps / consolidate versions / which package pulls this in". Always use this skill for dependency-hygiene requests; it maps the graph and recommends consolidation.
category: DevOps
version: 1.0.0
---

# Dependency Analyzer

Bring order to a project's or monorepo's dependencies: spot version drift and transitive conflicts, find unused and duplicate packages, and consolidate versions into one source of truth. For CVEs, hand off to `dependency-vuln-scanner`.

## Input - point it at your solution / repo
Works on a target: a **solution**, **monorepo**, **project**, or **GitHub URL** (clone first: `git clone --depth 1 <url> /tmp/repo`). Analyze the whole repo, not one project, so drift across projects is visible.

## Step 1 - detect the ecosystem(s)

| Manifest / lock file | Ecosystem | Centralization mechanism |
|---|---|---|
| `*.csproj`, `*.sln(x)`, `Directory.Packages.props`, `packages.lock.json` | NuGet | Central Package Management |
| `package.json` + `package-lock.json` / `pnpm-lock.yaml` / `yarn.lock` | npm / pnpm / yarn | workspaces, pnpm catalogs, `overrides` / `pnpm.overrides` / `resolutions` |
| `pyproject.toml` + `uv.lock` / `poetry.lock`, `requirements*.txt` | uv / poetry / pip | uv workspaces, constraints files |
| `pom.xml` | Maven | parent POM `dependencyManagement`, BOMs |
| `build.gradle(.kts)`, `gradle/libs.versions.toml` | Gradle | version catalogs, `platform(...)` |
| `go.mod`, `go.work` | Go modules | single module or `go.work` workspace |
| `Cargo.toml`, `Cargo.lock` | Cargo | `[workspace.dependencies]` |

Monorepos often mix several - analyze each ecosystem separately and report per ecosystem.

## What it finds (universal)
1. **Version drift** - the same package at different versions across projects/packages. A top source of runtime binding surprises and "works in one service, not the other". Consolidate to one.
2. **Transitive conflicts** - diamond dependencies resolved to a version nobody chose (nearest-wins in Maven/npm, highest-wins in Gradle/NuGet/Go MVS). Show the path that pulls it in.
3. **Duplicates** - multiple copies/majors of one package in the tree (bundle size, memory, type mismatches in Node).
4. **Unused declared dependencies** - and the reverse: used but undeclared (works only via a transitive dependency, breaks when it moves).
5. **Misplaced scope** - test/build tools in runtime dependencies (`dependencies` vs `devDependencies`, Maven `test` scope, Gradle `testImplementation`, poetry/uv dev groups).
6. **Loose or missing pins** - no lock file committed, wildcard/unbounded ranges, `latest`.
7. **Inconsistent framework families** - mixed versions inside one family (Microsoft.Extensions.*, Spring Boot starters, @angular/*, AWS SDK modules).

## C# / .NET (NuGet)
```
find <target> -name "*.csproj" | xargs grep -h "PackageReference" | sort | uniq -c | sort -rn
dotnet list <target> package                          # per-project versions
dotnet list <target> package --include-transitive
dotnet list <target> package --outdated
```
Central Package Management - scaffold `Directory.Packages.props` at the repo root:
```xml
<Project>
  <PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Serilog.AspNetCore" Version="10.0.0" />
    <PackageVersion Include="FluentValidation" Version="11.11.0" />
  </ItemGroup>
</Project>
```
Then `.csproj` files use `<PackageReference Include="..." />` with no version. Optional: `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>` to pin transitive versions centrally. Unused packages: check that the package's namespaces are actually imported; keep shared build props in `Directory.Build.props`.

## TypeScript / Node (npm, pnpm, yarn)
```
npm ls <pkg>            /  pnpm why <pkg>  /  yarn why <pkg>     # who pulls it in
npm outdated            /  pnpm outdated -r
npm dedupe              /  pnpm dedupe
npx knip                # unused deps, files, exports (or depcheck)
```
- Monorepo: workspaces + one lock file; pnpm **catalogs** (`catalog:` in `pnpm-workspace.yaml`) for shared versions; syncpack to enforce consistent versions across packages.
- Force a transitive version: `overrides` (npm), `pnpm.overrides`, `resolutions` (yarn). Document why each override exists.
- Check `peerDependencies` warnings - they are often the real conflict.

## Python (uv, poetry, pip)
```
uv tree                 /  poetry show --tree  /  pipdeptree
uv pip list --outdated  /  poetry show --outdated  /  pip list --outdated
deptry .                # unused, missing, transitive, misplaced dev deps
```
- Commit the lock file (`uv.lock` / `poetry.lock`); for pip, compile pins with pip-tools (`pip-compile`) or `uv pip compile`.
- Monorepo: uv workspaces share one lock; for pip, a shared constraints file (`-c constraints.txt`).
- Dev tools belong in dependency groups (`[dependency-groups]` / poetry groups), not runtime deps.

## Java / Kotlin (Maven, Gradle)
```
mvn dependency:tree -Dverbose            # shows omitted-for-conflict versions
mvn dependency:analyze                   # used-undeclared + unused-declared
mvn versions:display-dependency-updates
gradle dependencies --configuration runtimeClasspath
gradle dependencyInsight --dependency <name>
```
- Maven: centralize in the parent POM `<dependencyManagement>`; import BOMs (`spring-boot-dependencies`, Jackson, Netty) with `<scope>import</scope>`; enforce with maven-enforcer-plugin `dependencyConvergence`.
- Gradle: version catalog `gradle/libs.versions.toml`, `platform(...)` for BOMs, `constraints {}` for transitive pins, optional `failOnVersionConflict()`.
- Spring Boot: let the Boot BOM own versions; overriding one family member (e.g. Jackson) without the rest is a common drift source.

## Go
```
go mod graph | grep <module>
go mod why -m <module>
go list -m -u all          # available updates
go mod tidy                # removes unused, adds missing
```
- Go picks the minimum version satisfying all requirements (MVS) - drift shows as unexpected upgrades. Pin with `go get module@version`; avoid long-lived `replace` directives.
- Multi-module repos: `go.work` for local development; keep each module's `go.mod` tidy and committed with `go.sum`.

## Rust (Cargo)
`cargo tree -d` lists duplicate crates; centralize in `[workspace.dependencies]` and use `dep = { workspace = true }`; `cargo machete` or `cargo udeps` for unused crates.

## Output format
```
## Summary
<ecosystems detected, projects/packages scanned, headline issues>

## Findings
1. [Drift] <package> - <versions per project> -> consolidate to <version>
2. [Transitive] <package@version> via <path> -> <pin/upgrade parent>
3. [Unused] <project>: <package> -> remove (verify with build)

## Centralization
<Directory.Packages.props / catalog / dependencyManagement / workspace snippet, ready to paste>

## Verify on your side
<build + tests after consolidation; anything static analysis can't prove>
```

## Principles
- **One version per package across the repo** - drift causes the worst, hardest-to-repro bugs.
- **Centralize versions** (CPM, catalogs, BOMs, workspace deps) - it's the cure for version sprawl.
- **Commit lock files** for applications; reproducible builds are a prerequisite for any analysis.
- Remove what you don't use - smaller attack surface, faster restores and installs.
- Pair with the vuln scanner: consolidation makes patching one place, not ten.

## How to use it & best prompts
"Analyze my solution's NuGet dependencies", "set up Central Package Management", "fix version conflicts across my pnpm workspace", "why is this package in my node_modules twice", "find unused dependencies in my Python project", "converge versions in my Gradle multi-project build". Pairs with `dependency-vuln-scanner`.
