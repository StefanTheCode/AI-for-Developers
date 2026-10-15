---
name: secrets-config-auditor
description: Audit a codebase in any stack for hardcoded secrets and unsafe configuration - connection strings, API keys, passwords, tokens, and private keys in source, appsettings.json, .env, application.yml/properties, settings.py, docker-compose, Dockerfiles, and CI YAML - and recommend secret-store fixes. Use this skill whenever the user wants to find hardcoded secrets, leaked credentials, secrets in config or .env files, config security, debug/sensitive-logging flags left on, or asks "do I have secrets in my code / is my config safe to commit / where should my credentials live". Always use this skill for secrets/config audits; it scans and recommends a secret store.
category: Security
version: 1.0.0
---

# Secrets & Config Auditor

Find credentials and secrets that shouldn't be in source or committed config, flag unsafe config switches, and move secrets to a proper secret store.

## Input - point it at your project / repo
Works on a target: a **project**, **repo**, or **GitHub URL** (clone first: `git clone --depth 1 <url> /tmp/repo`). Scan source, config, infra, and pipeline files.

## Step 1 - detect the stack (decides which config files matter)

| Manifest | Config files to check | Dev secret store |
|---|---|---|
| `*.csproj` | `appsettings*.json`, `launchSettings.json`, `web.config`, `*.csproj` | `dotnet user-secrets` |
| `package.json` | `.env*`, `config/*.json`, `next.config.*`, `vite.config.*` | git-ignored `.env.local` + dotenv |
| `pyproject.toml`, `requirements*.txt` | `settings.py`, `.env`, `config.py`, `alembic.ini` | git-ignored `.env` (python-dotenv / pydantic-settings) |
| `pom.xml`, `build.gradle(.kts)` | `application*.yml`, `application*.properties`, `gradle.properties` | env vars / git-ignored profile file |
| `go.mod` | `config.yaml`, `.env`, `const` blocks in `*.go` | env vars / git-ignored `.env` |

Always also check: `Dockerfile*`, `docker-compose*.yml`, Kubernetes manifests / Helm values, Terraform (`*.tfvars`, state files), `.github/workflows`, `azure-pipelines.yml`, `.gitlab-ci.yml`, `.npmrc`/`.pypirc`/`nuget.config` (registry tokens), and `.gitignore`.

```
grep -rniE "password\s*[:=]|pwd=|api[_-]?key|secret|connectionstring|bearer [a-z0-9]|token\s*[:=]|accesskey|private[_-]?key|BEGIN (RSA|EC|OPENSSH) PRIVATE KEY" \
  --include=*.cs --include=*.json --include=*.config --include=*.ts --include=*.js --include=*.py \
  --include=*.java --include=*.kt --include=*.go --include=*.yml --include=*.yaml --include=*.properties \
  --include=*.env --include=*.tf <target> | grep -viE "/(obj|bin|node_modules|vendor|\.venv|target|build)/"
grep -rnE "AKIA[0-9A-Z]{16}|ghp_[A-Za-z0-9]{36}|xox[baprs]-|sk_live_|-----BEGIN" <target>   # well-known token shapes
git -C <target> ls-files | grep -E "(^|/)\.env($|\.)|\.pem$|\.pfx$|\.key$|tfstate"           # committed secret files
```
Prefer a real scanner when available: **gitleaks** (`gitleaks detect --source <target>`, scans history), **trufflehog** (verifies live credentials), or **detect-secrets**. Treat a match as a finding only after reading it: placeholders, test fixtures, and env-var references are not leaks.

## What it flags (universal)
1. 🔴 **Hardcoded secrets in source** - keys/passwords/tokens/private keys in code.
2. 🔴 **Secrets in committed config** - connection strings with passwords, API keys, signing keys in any config file listed above.
3. 🔴 **Secrets in pipeline YAML, Dockerfiles, compose, IaC** - `ENV`/`ARG` with secrets bakes them into image layers; should be platform secret variables, BuildKit `--mount=type=secret`, or runtime injection.
4. 🔴 **Committed secret files** - `.env`, `.pem`, `.pfx`, `terraform.tfstate`, registry auth files.
5. 🟡 **Unsafe config switches** - see per-stack list below.
6. 🟡 **Weak hygiene** - no `.gitignore` coverage for `.env`/local overrides, one config file with prod credentials for every environment, secrets logged at startup.

## Per-stack unsafe config switches
- **C#/.NET:** unconditional `EnableSensitiveDataLogging()` / `EnableDetailedErrors()` (leaks parameter values); `UseDeveloperExceptionPage()` outside Development; secrets in `launchSettings.json`; `TrustServerCertificate=True` in prod connection strings.
- **Node/TypeScript:** secrets in browser-exposed env vars (`NEXT_PUBLIC_*`, `VITE_*`, `REACT_APP_*` are bundled into client JS); `.npmrc` with `_authToken` committed; logging `process.env` or full request headers.
- **Python:** Django `DEBUG = True` / hardcoded `SECRET_KEY` / `ALLOWED_HOSTS = ["*"]` in settings; Flask `SECRET_KEY` literal or `debug=True`; SQLAlchemy URL with inline password.
- **Java/Spring:** `management.endpoints.web.exposure.include=*` (exposes `/env`, `/heapdump`); `spring.datasource.password` in committed `application.yml`; `spring.jpa.show-sql` with sensitive data in prod.
- **Go:** secrets in `const`/`var` or default flag values; `-ldflags "-X main.apiKey=..."` baking secrets into binaries; logging config structs that include credentials.

## Fix recommendations
- **Dev:** a local, git-ignored store - `dotnet user-secrets`, `.env.local` + dotenv, `.env` + pydantic-settings/python-dotenv, Spring profile file or env vars, direnv. Commit a `.env.example` with names only.
- **Prod:** Azure Key Vault / AWS Secrets Manager / GCP Secret Manager / HashiCorp Vault, surfaced through the framework's config providers (Key Vault configuration provider, Spring Cloud Vault / Spring Cloud Azure / Spring Cloud AWS, External Secrets Operator or CSI driver on Kubernetes) or env vars.
- **CI:** platform secret variables (GitHub Actions secrets, Azure DevOps variable groups linked to Key Vault, GitLab masked/protected variables); prefer OIDC federation to cloud providers over long-lived keys.
- **Containers:** inject at runtime; for build-time secrets use `RUN --mount=type=secret`.
- **Rotate anything that was committed** - it's compromised even after removal (git history, forks, CI logs). Then purge history only if needed (git filter-repo) - rotation comes first.
- Add patterns to `.gitignore`; add a pre-commit scanner (gitleaks hook) and enable GitHub secret scanning / push protection where available.

## Output format
```
## Summary
<stack, files scanned, count by severity>

## Findings
1. 🔴 [Hardcoded secret] <file:line> - <what, value redacted> -> <where it should live>
2. 🟡 [Unsafe switch] <file:line> - <setting> -> <fix>

## Fix plan
<dev store, prod store, CI, rotation list, .gitignore + pre-commit additions>
```
Never print a full secret value in the report - redact to the first 4 characters.

## Principles
- **A secret in source/committed config is a leaked secret** - and history keeps it. Rotate, don't just delete.
- Config should reference secrets, not contain them.
- Different environments, different stores - never one committed file with prod creds.
- Validate that required secrets are present at startup (`typed-config-generator`) so a missing secret fails the boot, not the first request.

## How to use it & best prompts
"Do I have any hardcoded secrets", "scan my repo for leaked credentials", "is my appsettings.json / .env / application.yml safe to commit", "where should these connection strings live". Pairs with the `api-security-auditor` agent and `typed-config-generator`.
