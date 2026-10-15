# How to use - Secrets & Config Auditor

**What it is.** Scans a codebase in any stack for hardcoded secrets and unsafe config - keys, passwords, tokens, and private keys in source, `appsettings.json`, `.env`, `application.yml`, `settings.py`, Dockerfiles, compose files, IaC, and pipeline YAML - plus risky switches like `EnableSensitiveDataLogging`, Django `DEBUG = True`, Spring actuator `include=*`, or secrets in `NEXT_PUBLIC_*` vars. Then it recommends the right secret store for dev, prod, and CI.

**When to reach for it.** Before open-sourcing, during a security pass, before a first deploy, or when unsure what's safe to commit.

**How to use it.** Point it at your project or repo (path or GitHub URL). Example prompts:
- "Do I have any hardcoded secrets?"
- "Is my appsettings.json safe to commit?"
- "Scan this Next.js app - am I leaking keys to the browser?"
- "Check my Django settings and .env handling."
- "Where should the datasource password in my Spring Boot app live?"

**Get the best out of it.** Point it at the whole repo, including config, Docker, IaC, and pipeline files - that's where most leaks hide. If it finds a committed secret, rotate it (git history keeps it). Ask for the pre-commit hook setup to stop the next leak.

**What you get.** Findings by file and severity (values redacted), and a fix plan: dev store, prod store, CI secrets, rotation list, `.gitignore` and pre-commit additions.

**Won't do.** It scans the working tree; deep git-history scanning and live-credential verification are jobs for gitleaks / trufflehog, which it will recommend and can run if installed. Pairs with `api-security-auditor` and `typed-config-generator`.
