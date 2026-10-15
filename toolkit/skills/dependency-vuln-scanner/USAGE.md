# How to use - Dependency Vulnerability Scanner

**What it is.** Scans dependencies (direct + transitive) for known CVEs, deprecation, and staleness in any ecosystem, then recommends the minimal safe upgrade path. It uses the native tool for your stack - `dotnet list package --vulnerable` / NuGetAudit, `npm`/`pnpm`/`yarn audit`, `pip-audit`, OWASP Dependency-Check, `govulncheck`, `cargo audit` - or a cross-ecosystem scanner (OSV-Scanner, Trivy, Grype) for polyglot repos.

**When to reach for it.** Security review, pre-release checks, after a CVE or Dependabot alert, or when `npm audit` turns red and you need to know what actually matters.

**How to use it.** Point it at your project/solution/repo (path or GitHub URL). Example prompts:
- "Scan my .NET solution for vulnerable packages, including transitive."
- "npm audit shows 40 issues - which ones matter and how do I fix them?"
- "Run pip-audit on my uv project."
- "This transitive Jackson CVE is flagged in my Spring app - how do I fix it?"
- "Run govulncheck and tell me what's reachable."

**Get the best out of it.** Let it run a restore/install so the scan is real, not version-guessing. Ask for the minimal safe bump to limit regression risk. Ask it to wire the scan into CI and set up Dependabot/Renovate.

**What you get.** A severity-ranked vulnerability table (advisory, fixed version, transitive path), deprecated/outdated packages, an ordered upgrade plan, and what to verify.

**Won't do.** Offline, it can only flag known-risky versions and marks results "verify". It won't silently apply major-version jumps. Pairs with `dependency-analyzer`.
