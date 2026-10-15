# How to use - Dependency Analyzer

**What it is.** Analyzes a project's or monorepo's dependencies - version drift across projects, transitive conflicts, duplicates, unused or misplaced packages, missing lock files - and consolidates versions into one source of truth. Works across ecosystems: NuGet with Central Package Management, npm/pnpm/yarn (workspaces, catalogs, overrides), uv/poetry/pip, Maven (BOMs, `dependencyManagement`) and Gradle (version catalogs), Go modules, and Cargo.

**When to reach for it.** Version conflicts, "works on my machine" binding issues, a package installed twice, or cleaning up dependency sprawl in a multi-project repo.

**How to use it.** Point it at your solution or repo (path or GitHub URL). Example prompts:
- "Analyze my solution's NuGet dependencies and set up Central Package Management."
- "Fix version drift across my pnpm workspace."
- "Why is lodash in node_modules three times?"
- "Find unused dependencies in my uv project."
- "Converge dependency versions in my Gradle multi-project build."
- "Which module pulls in this old version in my Go service?"

**Get the best out of it.** Point it at the whole repo so it sees drift across projects. Make sure lock files are committed. Ask for the centralization scaffold (`Directory.Packages.props`, pnpm catalog, version catalog, parent POM) to lock versions in one place.

**What you get.** A summary per ecosystem, findings (drift, transitive, unused, misplaced), a ready-to-paste centralization snippet, and what to verify.

**Won't do.** It analyzes versions and usage statically; confirm runtime behavior with a build and tests after consolidating. For CVEs, use `dependency-vuln-scanner`.
