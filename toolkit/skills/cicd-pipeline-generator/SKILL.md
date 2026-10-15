---
name: cicd-pipeline-generator
description: Generate a CI/CD pipeline for an app in any stack - GitHub Actions (primary), Azure DevOps, or GitLab CI - with per-stack install/build/test/coverage, dependency caching, lint, vulnerability scan, container build and push, and environment-gated deploy. Covers .NET, Node/TypeScript (npm/pnpm/yarn), Python (pip/uv/poetry), Java/Kotlin (Maven/Gradle), Go, and monorepos with several stacks. Use this skill whenever the user wants a CI/CD pipeline, a GitHub Actions workflow, Azure DevOps YAML, .gitlab-ci.yml, build/test automation, a release or deploy pipeline, caching to speed up CI, or asks "how do I set up CI / pipeline for my app / automate build and deploy / why is my CI slow". Always use this skill for CI/CD requests; it generates a complete, sensible pipeline.
category: DevOps
version: 1.0.0
---

# CI/CD Pipeline Generator

Generate a complete CI/CD pipeline - GitHub Actions by default, Azure DevOps or GitLab CI on request - covering install, build, test with coverage, security scan, container image, and gated deploy, using each stack's idiomatic commands and caching.

## Input - point it at your project
Works on a target: a **repo** or **GitHub URL** (clone first: `git clone --depth 1 <url> /tmp/repo`). Detect:
- **Platform:** existing `.github/workflows/`, `azure-pipelines.yml`, `.gitlab-ci.yml` (extend what exists; ask if none).
- **Stack(s), versions, package manager, test projects**, and whether there's a `Dockerfile` and a deploy target.
```
find <target> -maxdepth 3 \( -name "*.sln*" -o -name "*.csproj" -o -name package.json -o -name pyproject.toml \
  -o -name requirements.txt -o -name pom.xml -o -name "build.gradle*" -o -name go.mod -o -name Dockerfile \) -not -path "*/node_modules/*"
```

## Universal pipeline shape
1. **Triggers:** PRs to `main` for fast feedback; push to `main` (and tags) for release.
2. **CI job:** checkout -> setup toolchain (pinned version) -> restore with cache -> lint/format check -> build -> test with coverage -> dependency vuln scan -> upload test results/coverage.
3. **Container job** (if Dockerfile): build with layer cache, scan the image, push to a registry only from `main`/tags, tag with commit SHA (and semver on tags).
4. **Deploy job(s):** depend on CI + container, environment-gated (approvals for prod), deploy the exact image digest that was tested.

Rules:
- **Fail fast on quality + security** - tests, lint, and high/critical vulns break the build.
- **Cache dependencies keyed on the lock file**; don't re-restore between steps.
- **Least privilege:** `permissions: contents: read` by default; grant more per job. Prefer **OIDC** to cloud providers over long-lived keys.
- **Secrets from the platform store**, never YAML literals; never echo them.
- **Pin action/task versions** (major tag at least, commit SHA for third-party actions in high-security repos).
- `concurrency` to cancel superseded PR runs; a matrix only where it buys something (OS/runtime versions for libraries).

## GitHub Actions - workflow skeleton
```yaml
name: ci
on:
  push: { branches: [main], tags: ['v*'] }
  pull_request: { branches: [main] }
permissions:
  contents: read
concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true
jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      # <per-stack setup + steps from the section below>
  container:
    needs: build-test
    if: github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/tags/')
    runs-on: ubuntu-latest
    permissions: { contents: read, packages: write }
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with: { registry: ghcr.io, username: '${{ github.actor }}', password: '${{ secrets.GITHUB_TOKEN }}' }
      - uses: docker/build-push-action@v6
        with:
          push: true
          tags: ghcr.io/${{ github.repository }}:${{ github.sha }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
  deploy:
    needs: container
    runs-on: ubuntu-latest
    environment: production          # approvals configured on the environment
    steps:
      - run: echo "deploy ghcr.io/${{ github.repository }}:${{ github.sha }}"
```

## Per-stack CI steps (GitHub Actions)

**C# / .NET**
```yaml
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '9.0.x' }
      - uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props', '**/packages.lock.json') }}
      - run: dotnet restore
      - run: dotnet format --verify-no-changes --no-restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release --collect:"XPlat Code Coverage" --logger trx
      - run: dotnet list package --vulnerable --include-transitive   # make it fail on findings (grep or NuGetAudit as error)
      - uses: actions/upload-artifact@v4
        with: { name: coverage, path: '**/coverage.cobertura.xml' }
```
`setup-dotnet` also has built-in caching when `packages.lock.json` exists. Integration tests with Testcontainers run on `ubuntu-latest` (Docker available).

**TypeScript / Node**
```yaml
      - uses: actions/setup-node@v4
        with: { node-version-file: '.nvmrc', cache: 'npm' }    # or node-version: 22; cache: pnpm / yarn
      - run: npm ci
      - run: npm run lint
      - run: npm run build
      - run: npm test -- --coverage
      - run: npm audit --omit=dev --audit-level=high
```
pnpm: add `pnpm/action-setup` before `setup-node` and use `pnpm install --frozen-lockfile`.

**Python**
```yaml
      - uses: astral-sh/setup-uv@v6          # pin per your repo; enable-cache: true
        with: { enable-cache: true }
      - run: uv sync --frozen
      - run: uv run ruff check . && uv run ruff format --check .
      - run: uv run pytest --cov --cov-report=xml
      - run: uvx pip-audit                   # or export requirements and pip-audit -r
```
pip: `actions/setup-python@v5` with `cache: 'pip'`, `pip install -r requirements.txt`. poetry: `pipx install poetry`, `setup-python` with `cache: 'poetry'`, `poetry install`.

**Java / Kotlin**
```yaml
      - uses: actions/setup-java@v4
        with: { distribution: temurin, java-version: '21', cache: maven }   # or cache: gradle
      - run: ./mvnw -B verify                  # compile + unit + integration tests (failsafe) + JaCoCo if configured
```
Gradle: `gradle/actions/setup-gradle@v4` then `./gradlew build`. Vulnerability scan via the OWASP Dependency-Check plugin or a cross-ecosystem scanner (Trivy/OSV-Scanner).

**Go**
```yaml
      - uses: actions/setup-go@v5
        with: { go-version-file: go.mod }      # module + build cache on by default
      - run: go vet ./...
      - uses: golangci/golangci-lint-action@v6
      - run: go test -race -coverprofile=coverage.out ./...
      - run: go run golang.org/x/vuln/cmd/govulncheck@latest ./...
```

**Monorepo / polyglot:** one job per stack with `paths` filters (or `dorny/paths-filter`) so a frontend change doesn't rebuild the backend; a final required "all green" job for branch protection.

## Azure DevOps notes
- `azure-pipelines.yml` with `stages` (Build -> Container -> Deploy) and `deployment` jobs bound to **environments** for approvals/checks.
- Tasks: `UseDotNet@2` + `DotNetCoreCLI@2` (restore/build/test/publish), `NodeTool@0`, `UsePythonVersion@0`, `Maven@4` / `Gradle@3`, `GoTool@0`; `Cache@2` keyed on lock files; `PublishTestResults@2`, `PublishCodeCoverageResults@2`; `Docker@2` (buildAndPush) with a service connection.
- Secrets: variable groups (link to Key Vault), workload identity federation for Azure service connections.

## GitLab CI notes
- `.gitlab-ci.yml` with `stages: [build, test, scan, package, deploy]`; per-job `image:` (`mcr.microsoft.com/dotnet/sdk`, `node`, `python`, `eclipse-temurin`/`maven`, `golang`).
- `cache: key: files: [<lock file>]` with paths inside the project dir (e.g. `.npm/`, `.m2/repository`, `.cache/pip`, `.nuget/packages` via `NUGET_PACKAGES`).
- Test reports via `artifacts: reports: junit` and `coverage_report` (cobertura); `rules:` for PR (merge request) vs `main`.
- Secrets in masked/protected CI/CD variables; deploy jobs with `environment:` and `when: manual` for prod. GitLab's built-in dependency/container scanning templates are an option on supported tiers.

## Output format
```
## Detected
<platform, stack(s), versions, test projects, Dockerfile, deploy target>

## Pipeline
<full YAML file(s), path + content>

## Secrets & settings to configure
<secrets/variables, environments + approvals, registry, branch protection required checks>

## Notes
<caching, what fails the build, next steps>
```

## Principles
- **Fail fast on quality + security** - tests, lint, and vuln scans gate the merge.
- Keep CI fast: cache by lock file, `--no-restore`/`--no-build` style flags between steps, path filters in monorepos.
- Build once, deploy the same artifact/image digest to every environment.
- Secrets come from the platform; prefer OIDC over stored cloud keys.
- Gate deploys with environments/approvals; don't auto-ship to prod on every commit unless that's intended.

## How to use it & best prompts
"Set up GitHub Actions for my .NET app", "CI for my pnpm monorepo with caching", "GitHub Actions for a FastAPI app with uv and pytest", "Azure DevOps pipeline with tests and coverage", "GitLab CI for my Spring Boot service", "build and push a container, then deploy with approval". Pairs with `dockerfile-generator` and `dependency-vuln-scanner`.
