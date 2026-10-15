# How to use - Legacy Package Migrator

**What it is.** A skill that moves a .NET Framework solution off `packages.config` and onto `PackageReference`, then lifts every NuGet version into a single `Directory.Packages.props` using Central Package Management (CPM). It's built to kill version drift across a multi-project - and multi-vendor - solution.

**When to reach for it.** You're on `packages.config`, different projects reference different versions of the same package, you want to pin transitive versions once, or third-party teams build extensions against your solution and must all use the same package versions.

**How to use it.** Point it at your solution - no pasting needed. Give it a **repo root / `.sln`**, a **folder**, a single **`.csproj` + `packages.config`**, or a **GitHub URL** (it clones and scans). Example prompts:
- "Migrate this solution from packages.config to Central Package Management."
- "Different projects use different Newtonsoft versions - unify them."
- "Set up Directory.Packages.props and pin our transitive dependencies."

**How to get the best out of it.**
- **Let it do the two stages in order** - `packages.config` → `PackageReference` first (per project), *then* CPM. It refuses to jump straight to CPM while any project still has `packages.config`, on purpose.
- **Review the drift inventory it prints** before it writes `Directory.Packages.props` - it flags risky *major* version jumps for you to test instead of silently bumping.
- **Tell it if vendors build extensions** against the solution - it adds a guardrail note (and optional CI check) so nobody re-adds a per-project `Version=`.

**What it won't do.** It won't convert your projects to SDK-style `.csproj` as part of this (that's a separate, bigger step) unless you ask - `PackageReference` works fine in classic `.csproj`. It won't push a major version bump without flagging it for testing.
