# How to use - API Security Auditor (agent)

**What it is.** An agent that audits a backend or web API codebase in any stack - **ASP.NET Core, Express/NestJS, FastAPI/Django, Spring Boot, Go (net/http, Gin, Echo)** and others - against the **OWASP Top 10** and the **OWASP API Security Top 10**: broken access control and object-level authorization (IDOR/BOLA), auth and JWT flaws, injection, secrets in source, insecure config, CORS, mass assignment, SSRF, missing rate limits, and vulnerable dependencies. It produces a risk-ranked report where every finding shows the vulnerable code, the concrete attack it enables, and the fix in your framework.

**When to reach for it.** A pre-release security pass, a periodic review, after adding auth or new endpoints, or "is my API actually secure".

**How to use it.** Point it at the repo. It detects the stack and explores the attack surface itself (entry point, middleware, routes, config, dependency manifests). Example prompts:
- "Audit the security of this API."
- "Check my Express app against the OWASP API Security Top 10 before we ship."
- "Run a security review on this FastAPI service - focus on auth and access control."
- "Are there any secrets or IDOR issues in this Spring Boot repo?"

**How to get the best out of it.**
- **Give it the full repo including config files** - many findings live in config, CORS setup, and the app entry point, not just handlers.
- **Let it run the dependency audit** for your stack (`dotnet list package --vulnerable`, `npm audit`, `pip-audit`, OWASP Dependency-Check, `govulncheck`) so the CVE check is real, not version-guessing.
- **Focus it** if you want - "just access control and auth" - for a deeper pass on one area.
- **Act on the Quick Wins section first** - it's the highest-leverage subset.

**What it won't do.** It can't run the app, so some findings are marked "verify:" with how to confirm them. It's a code/config review, not a penetration test or DAST scan - treat it as the first line, not the only one. It won't invent vulnerabilities to pad the report.
