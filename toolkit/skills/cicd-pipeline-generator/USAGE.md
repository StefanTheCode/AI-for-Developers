# How to use - CI/CD Pipeline Generator

**What it is.** Generates a complete CI/CD pipeline for your stack - GitHub Actions by default, Azure DevOps or GitLab CI on request: install with lock-file caching, lint, build, test with coverage, dependency vulnerability scan, container build + push, and environment-gated deploy. It uses each stack's idiomatic toolchain - .NET (`setup-dotnet`, `dotnet test`), Node (npm/pnpm/yarn), Python (uv/pip/poetry + pytest), Java/Kotlin (Maven/Gradle), Go (`go test -race`, govulncheck) - and handles polyglot monorepos with path filters.

**When to reach for it.** Setting up CI for a repo, adding test/coverage/security gates, speeding up a slow pipeline, or adding a container build and gated deploy.

**How to use it.** Point it at your repo (path or GitHub URL). Example prompts:
- "Set up GitHub Actions for my .NET app."
- "CI for my pnpm monorepo - only rebuild what changed."
- "GitHub Actions for a FastAPI app with uv, ruff, and pytest."
- "GitLab CI for my Spring Boot service with Maven caching."
- "Azure DevOps pipeline with tests, coverage, and a Docker push."
- "Add a vulnerability scan that fails the build to my Go CI."

**Get the best out of it.** Let it detect your platform, stack versions, test projects, and Dockerfile so stages fit. Ask to gate deploys with environments/approvals and to use OIDC for cloud credentials.

**What you get.** The detected setup, full pipeline YAML file(s), the list of secrets/environments/branch-protection settings to configure, and notes on caching and gates.

**Won't do.** It won't put secrets in YAML - it wires them through the platform's secret store. It can't configure your cloud or registry accounts; it tells you exactly what to set up. Pairs with `dockerfile-generator` and `dependency-vuln-scanner`.
