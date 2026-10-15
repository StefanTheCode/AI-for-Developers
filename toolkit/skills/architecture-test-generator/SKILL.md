---
name: architecture-test-generator
description: Generate architecture tests / fitness functions that fail the build when architectural rules are violated - layer dependencies, module boundaries, cycles, naming, and visibility conventions - in any stack: NetArchTest or ArchUnitNET (.NET), ArchUnit or Konsist (Java/Kotlin), dependency-cruiser or eslint-plugin-boundaries (TypeScript/JavaScript), import-linter (Python), go-arch-lint or depguard (Go). Use this skill whenever the user wants architecture tests, to enforce layering or the dependency rule, dependency rules as tests, import rules, forbidden imports, no circular dependencies, naming conventions, fitness functions, or asks "how do I stop people breaking the architecture / enforce my layers / keep modules from importing each other". Always use this skill for architecture-test requests; it generates rules matching the project's real structure.
category: Testing
version: 1.0.0
---

# Architecture Test Generator

Turn architectural rules into automated checks so violations fail the build instead of slowly rotting the codebase - layering, dependencies, module boundaries, cycles, and naming rules tailored to the project.

## Input - point it at your project
Works on a target: a **solution/project/repo** or **GitHub URL** (`git clone --depth 1 <url> /tmp/repo`). First map the real structure: projects/packages, folder names, and who imports whom. Identify the style (Clean/Onion layers? Hexagonal? Vertical Slice? modular monolith?) before writing a single rule.

## Step 1 - detect the stack and pick the tool

| Manifest | Stack | Tool (prefer what's already installed) | Runs as |
|---|---|---|---|
| `*.csproj` / `*.sln` | C# / .NET | NetArchTest.Rules or ArchUnitNET | xUnit/NUnit/MSTest test |
| `pom.xml` / `build.gradle(.kts)` | Java / Kotlin | ArchUnit (Kotlin: also Konsist) | JUnit 5 test |
| `package.json` | TypeScript / JavaScript | dependency-cruiser, or eslint-plugin-boundaries (Nx: `@nx/enforce-module-boundaries`) | CLI / lint step in CI |
| `pyproject.toml` / `requirements*.txt` | Python | import-linter | `lint-imports` in CI |
| `go.mod` | Go | depguard (via golangci-lint) or go-arch-lint; plus Go's `internal/` package rule | lint step in CI |

Then map the rules to real names: assemblies/namespaces, Java packages, TS path globs, Python dotted modules, Go import paths.

## Universal rule catalog (pick the ones that match real decisions)
1. **Dependency rule (layers)** - inner layers (domain) never depend on outer ones (application, infrastructure, web).
2. **Framework isolation** - domain code imports no ORM, web framework, or HTTP client.
3. **Module boundaries** - a module only uses another module's public API/contracts, never its internals.
4. **No cycles** - between modules, packages, or layers.
5. **Entry points stay thin** - controllers/endpoints/handlers don't touch the DB layer directly.
6. **Naming and placement** - handlers end with `Handler`, repositories live in infrastructure, etc. Only where the team actually relies on it.
7. **Visibility** - internals stay internal/package-private (sealed handlers, `internal` types, Go `internal/`).

Each rule must map to a real architectural decision. A handful of high-value rules beats dozens of pedantic ones.

## C# / .NET - NetArchTest (or ArchUnitNET)
```
dotnet add package NetArchTest.Rules
```
**Clean Architecture dependency rule:**
```csharp
[Fact]
public void Domain_has_no_outward_dependencies()
{
    var r = Types.InAssembly(typeof(Order).Assembly)
        .Should().NotHaveDependencyOnAny("MyApp.Application", "MyApp.Infrastructure", "MyApp.Api")
        .GetResult();
    Assert.True(r.IsSuccessful, string.Join("\n", r.FailingTypeNames ?? new()));
}
```
**Other common fitness functions:**
```csharp
// Handlers must be sealed
Types.InAssembly(asm).That().HaveNameEndingWith("Handler").Should().BeSealed();
// Controllers/endpoints don't reference the DbContext directly
Types.InAssembly(asm).That().HaveNameEndingWith("Controller").ShouldNot().HaveDependencyOn("MyApp.Infrastructure.Data");
// Modules don't reference each other's internals (modular monolith)
Types.InAssembly(ordersAsm).ShouldNot().HaveDependencyOn("MyApp.Modules.Billing");
```
Always print `FailingTypeNames` in the assertion message so the failure is actionable. Use ArchUnitNET instead if it's already in the solution or you need its richer layer/slice API.

## Java / Kotlin - ArchUnit
```java
@AnalyzeClasses(packages = "com.acme.shop")
class ArchitectureTest {
    @ArchTest
    static final ArchRule layers = layeredArchitecture().consideringAllDependencies()
        .layer("Web").definedBy("..web..")
        .layer("Application").definedBy("..application..")
        .layer("Domain").definedBy("..domain..")
        .layer("Infrastructure").definedBy("..infrastructure..")
        .whereLayer("Web").mayNotBeAccessedByAnyLayer()
        .whereLayer("Application").mayOnlyBeAccessedByLayers("Web", "Infrastructure")
        .whereLayer("Domain").mayOnlyBeAccessedByLayers("Application", "Infrastructure");

    @ArchTest
    static final ArchRule domainIsFrameworkFree = noClasses().that().resideInAPackage("..domain..")
        .should().dependOnClassesThat().resideInAnyPackage("org.springframework..", "jakarta.persistence..");

    @ArchTest
    static final ArchRule modulesIndependent = slices().matching("com.acme.shop.modules.(*)..")
        .should().notDependOnEachOther();
}
```
On Spring Modulith projects, `ApplicationModules.of(App.class).verify()` already enforces module boundaries - use it instead of hand-written slice rules. For Kotlin-specific conventions (naming, file structure) Konsist is an option.

## TypeScript / JavaScript - dependency-cruiser
```js
// .dependency-cruiser.js
module.exports = {
  forbidden: [
    { name: "no-circular", severity: "error", from: {}, to: { circular: true } },
    { name: "domain-is-pure", severity: "error",
      from: { path: "^src/domain" }, to: { path: "^src/(infrastructure|api)" } },
    { name: "domain-no-orm", severity: "error",
      from: { path: "^src/domain" }, to: { path: "node_modules/(@prisma|typeorm|drizzle-orm)" } },
    { name: "modules-via-public-api", severity: "error",
      from: { path: "^src/modules/([^/]+)/" },
      to: { path: "^src/modules/([^/]+)/(?!index\\.ts$)", pathNot: "^src/modules/$1/" } },
  ],
};
```
Run `npx depcruise src` in CI (generate a starter config with `npx depcruise --init`). If the project lints rather than tests, eslint-plugin-boundaries expresses the same rules as element types with allowed dependencies; in Nx workspaces use `@nx/enforce-module-boundaries` with project tags. Check the rule names against the installed plugin version.

## Python - import-linter
```toml
# pyproject.toml
[tool.importlinter]
root_package = "shop"

[[tool.importlinter.contracts]]
name = "Layered architecture"
type = "layers"
layers = ["shop.api", "shop.application", "shop.domain"]

[[tool.importlinter.contracts]]
name = "Domain is framework-free"
type = "forbidden"
source_modules = ["shop.domain"]
forbidden_modules = ["sqlalchemy", "fastapi", "django"]

[[tool.importlinter.contracts]]
name = "Modules are independent"
type = "independence"
modules = ["shop.modules.orders", "shop.modules.billing"]
```
Run `lint-imports` in CI. Forbidding third-party modules requires `include_external_packages = true` in the `[tool.importlinter]` section.

## Go - depguard / go-arch-lint
Go already enforces one boundary for free: a package under `x/internal/` can only be imported by code rooted at `x/`. Use it for module internals first. For layer rules, depguard via golangci-lint:
```yaml
# .golangci.yml (depguard v2 rules; golangci-lint v2 nests this under linters.settings)
linters-settings:
  depguard:
    rules:
      domain:
        files: ["**/internal/domain/**"]
        deny:
          - pkg: "github.com/acme/shop/internal/infrastructure"
            desc: "domain must not depend on infrastructure"
          - pkg: "gorm.io/gorm"
            desc: "domain must stay persistence-agnostic"
```
For a whole-graph component model (components + `mayDependOn`), go-arch-lint with a `.go-arch-lint.yml` is the closer equivalent of ArchUnit; verify the config schema against its docs for the installed version.

## Output format
```
## Detected structure
<stack, tool chosen, architecture style, layers/modules found>

## Rules
| Rule | Why it matters | Currently passing? |

## Files
<test class / config file(s), ready to paste>

## CI
<the command or test run that enforces it>
```
Run the rules against the current code when possible. If a rule fails today, report the existing violations instead of silently weakening the rule.

## Principles
- **Encode the rules you actually care about** - every rule maps to a real architectural decision.
- Architecture tests are **fitness functions**: cheap, fast, run in CI, fail loudly with the offending type/file.
- Match the rules to the real structure (don't impose Clean rules on a Vertical Slice app).
- Prefer the tool already in the repo; don't add a second one.

## How to use it & best prompts
"Add architecture tests to enforce my layers", "stop controllers from using the DbContext", "enforce that handlers are sealed", "ArchUnit rules for my Spring modules", "dependency-cruiser rules so domain can't import infrastructure", "import-linter contracts for my Django apps", "depguard rules for my Go service". Pairs with `clean-architecture-scaffolder`, `modular-monolith-generator`, and the `architecture-reviewer` agent.
