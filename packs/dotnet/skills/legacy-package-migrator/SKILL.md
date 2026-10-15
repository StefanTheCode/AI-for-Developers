---
name: legacy-package-migrator
description: (.NET) Migrate a .NET Framework solution from packages.config to PackageReference and Central Package Management (Directory.Packages.props). Use this skill whenever the user mentions packages.config, PackageReference, "migrate packages", Directory.Packages.props, Central Package Management / CPM, transitive package versions, NuGet version drift across projects, "different projects use different versions of the same package", or wants to unify NuGet versions across a multi-project solution (including third-party/vendor extension projects). Always use this skill for these requests; it produces a staged, per-project migration plan plus the actual Directory.Packages.props and .csproj edits, not a generic explanation.
category: Legacy (.NET)
---

# Legacy Package Migrator (packages.config -> PackageReference -> CPM)

Move a .NET Framework solution off `packages.config` and onto `PackageReference`, then lift all versions into a single `Directory.Packages.props` using Central Package Management (CPM). The goal is one source of truth for every NuGet version across every project - including third-party extension projects - so nobody ends up with two versions of the same package in one solution.

## Input - point it at your code

You don't need pasted files. Work from whatever target the user gives:

- **A solution / repo root** - the `.sln` and every project under it
- **A folder** - scan for `packages.config`, `*.csproj`, `app.config`/`web.config`
- **A single project** - one `.csproj` + its `packages.config`
- **A GitHub URL** - clone first: `git clone --depth 1 <url> /tmp/repo && cd /tmp/repo`

Discover the packaging state yourself with Glob/Grep/Bash instead of asking for a paste:
```
grep -rl "packages.config" <target> | grep -vi "/obj/\|/bin/"
# version drift across the solution:
grep -rhn "id=\"Newtonsoft.Json\"" --include=packages.config <target>
grep -rn "<PackageReference" --include=*.csproj <target>
```
Tell the user which projects you scanned, and always show the drift inventory before writing `Directory.Packages.props`.

## When this runs

The user has a `packages.config`-based solution (usually .NET Framework, often many projects) and wants to modernize package management, pin transitive versions, or stop version drift across modules and vendor extensions. They may point at a folder, paste a `.csproj` / `packages.config`, or just describe the mess.

If you don't have the files, ask for: the solution's project list, one representative `.csproj` + its `packages.config`, and whether third-party/vendor projects live in the same solution or reference the same packages.

## Why this order matters

Do it in two stages, never one. Each stage is independently verifiable and revertible:

1. **Stage 1 - `packages.config` -> `PackageReference`** (per project). Behavior-preserving; versions still live in each `.csproj`.
2. **Stage 2 - `PackageReference` -> Central Package Management.** Hoist every version into one `Directory.Packages.props`; strip versions from projects.

Doing Stage 2 before every project is on `PackageReference` will silently skip the leftover `packages.config` projects and leave you with a split-brain solution.

## Stage 1 - packages.config -> PackageReference

### Automated path (do this first)
Visual Studio: right-click the project -> **Migrate packages.config to PackageReference**. It's the fastest correct starting point. Then verify by hand with the checklist below, because the migrator gets three things wrong often enough that you must check every time.

### What the migrator gets wrong (verify every project)

1. **Transitive dependencies promoted to top-level.** `packages.config` listed every transitive package as if it were direct. The migrator copies all of them into `<PackageReference>`, so a project that directly uses only `Serilog` now also "directly" references `Serilog.Sinks.File`, `System.Text.Json`, etc.
   - **Fix:** keep only the packages the project's code actually references directly; let the rest come in transitively. When you genuinely need to pin a transitive version, do it in CPM (Stage 2) with transitive pinning - not as a fake direct reference in every project.

2. **`packages.config`, `app.config` binding redirects, and the `.nuget` targets are left behind.**
   - **Fix:** delete `packages.config`. Delete the auto-generated `<bindingRedirect>` block from `app.config`/`web.config` if present - with `PackageReference` on modern MSBuild, redirects are generated at build (`AutoGenerateBindingRedirects`). Remove any `<Import>` of `NuGet.targets` / `packages\...\build\...` from the `.csproj`.

3. **`ExcludeAssets` / `PrivateAssets` not set for build/analyzer/dev-only packages.**
   - **Fix:** analyzers, `Microsoft.SourceLink.*`, `*.Build.Tasks`, StyleCop, etc. should carry `PrivateAssets="all"` so they don't flow to consumers.

### Per-project checklist (Stage 1)
- [ ] Only direct dependencies remain as `<PackageReference>` (transitives removed).
- [ ] `packages.config` deleted from disk **and** from the project file.
- [ ] Hand-written binding redirects removed; `<AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>` present for exe/test projects on .NET Framework.
- [ ] `PrivateAssets="all"` on analyzers / build-only / dev-only packages.
- [ ] Project builds and restores clean before moving on.

> Tip: SDK-style `.csproj` is *not* required for `PackageReference` - a classic `.csproj` can use `PackageReference` too. Converting to SDK-style is a separate, larger step; don't couple it to this migration unless the user asks.

## Stage 2 - PackageReference -> Central Package Management (CPM)

This is the part that actually kills version drift across a multi-project / multi-vendor solution.

### 1. Enable CPM at the repo root
Create `Directory.Packages.props` next to the `.sln`:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <!-- Let a central version pin transitive dependencies too -->
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <!-- ONE version per package, for the WHOLE solution -->
    <PackageVersion Include="Serilog" Version="3.1.1" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageVersion Include="Dapper" Version="2.1.35" />
  </ItemGroup>
</Project>
```

### 2. Strip versions from every project
Each `.csproj` keeps the reference, drops the version:

```xml
<!-- Before -->
<PackageReference Include="Serilog" Version="2.10.0" />
<!-- After (version now comes from Directory.Packages.props) -->
<PackageReference Include="Serilog" />
```

### 3. Resolve the drift (the whole point)
Before writing `Directory.Packages.props`, inventory every version of every package across all projects, then pick **one** per package:

```
Package             Versions found in solution     -> Chosen
Newtonsoft.Json     12.0.3, 13.0.1, 13.0.3         -> 13.0.3
Serilog             2.10.0, 3.1.1                  -> 3.1.1
System.Text.Json    4.7.2, 6.0.0, 8.0.4           -> 8.0.4 (verify - major jumps)
```
- Prefer the **highest version already in use** unless a major jump has breaking changes - flag those explicitly for the user to test, don't silently bump.
- `System.*` / `Microsoft.Extensions.*` packages: keep them aligned with each other and with the target framework.

### 4. Transitive pinning (the classic legacy problem)
With `CentralPackageTransitivePinningEnabled=true`, you can pin a transitive dependency's version once, centrally, and it applies everywhere - no fake direct references, no per-project edits. This is how you guarantee that every project (and every vendor extension project that builds in this solution) restores the *same* version of a shared transitive package.

For build/tooling packages that every project should carry identically, use:
```xml
<GlobalPackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" />
```
(applies to all projects automatically; good for analyzers, SourceLink, banned-API analyzers).

### 5. Multi-vendor guardrail
When other companies build extension projects against the same solution, add a short `NuGet.md` at the repo root stating: "Versions are managed centrally in `Directory.Packages.props`. Do **not** put `Version=` on a `PackageReference`. To change a version, edit `Directory.Packages.props` and open a PR." Optionally enforce with an analyzer/CI check that fails on any `PackageReference` carrying a `Version` attribute.

### Solution-wide checklist (Stage 2)
- [ ] `Directory.Packages.props` at repo root with `ManagePackageVersionsCentrally=true`.
- [ ] Exactly one `<PackageVersion>` per package ID - no duplicates.
- [ ] Every project's `<PackageReference>` has **no** `Version` attribute.
- [ ] No `packages.config` remains anywhere (`git grep -l packages.config`).
- [ ] `CentralPackageTransitivePinningEnabled=true` if you need to pin transitives.
- [ ] `dotnet restore` / VS restore succeeds with **no NU1605 downgrade** and no version-conflict warnings.
- [ ] `dotnet list <proj> package --include-transitive` shows the versions you expect.

## Output format

```
## Inventory
<table: package -> versions found across projects -> chosen version, with any risky major jumps flagged>

## Stage 1 - per project
<for each project: what to remove (transitives, packages.config, redirects), what to keep>

## Stage 2 - Directory.Packages.props
<the full generated file, ready to drop at repo root>

## Project edits
<the exact PackageReference lines to change, per csproj>

## Verify on your side
- restore with no NU1605 / version-conflict warnings
- dotnet list package --include-transitive spot check
- vendor guardrail note
```

## Principles
- **Two stages, verified independently.** Never jump to CPM while any project still has `packages.config`.
- **Don't promote transitives to direct references.** That's the #1 mess the VS migrator creates - clean it up.
- **Highest-in-use wins, majors get flagged.** Never silently bump across a major version; call it out for testing.
- **One version, one place, enforced.** The value of CPM is gone the moment a project pins its own version - block it.
- **Don't couple SDK-style conversion to this.** `PackageReference` works in classic `.csproj`; keep the migration small and revertible.
