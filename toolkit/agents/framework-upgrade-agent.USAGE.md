# How to use - Framework Upgrade Agent

**What it is.** An agent that plans and (with approval) executes a runtime or framework major-version upgrade in any stack - **.NET** (TFM bumps, .NET Framework to modern .NET, Upgrade Assistant), **Node.js** LTS and major dependency bumps, **Python** versions (pyupgrade, Pydantic/SQLAlchemy/Django majors), **Java** LTS and **Spring Boot** (OpenRewrite, javax to jakarta), and **Go** versions. It handles breaking-change remediation, deprecated-API replacement, dependency updates, version pins in CI and Docker, and per-stage verification.

**When to reach for it.** Moving to a newer runtime or framework major, migrating target frameworks, or "what breaks if I upgrade".

**How to use it.** Point it at your codebase (path or GitHub URL). Example prompts:
- "Upgrade this solution from .NET 6 to .NET 10."
- "Plan our move from .NET Framework 4.8 to modern .NET."
- "Move this service from Node 18 to Node 22 and bump NestJS to the latest major."
- "Upgrade this project to Python 3.13 and modernize the syntax."
- "Take this app from Spring Boot 2.7 / Java 11 to Spring Boot 3 / Java 21."
- "Bump this Go module to the latest Go release and update dependencies."

**Get the best out of it.** Let it upgrade one step at a time and verify (build + tests) between steps. Review its plan before it edits - it asks first. It runs the ecosystem's automated tooling first and fixes the rest by hand. For legacy .NET Framework apps it pairs with `legacy-modernization-assistant` and the .NET pack's `legacy-package-migrator`.

**Won't do.** It won't power through a red build or skip verification to finish faster, and it defers deep rewrites to `legacy-modernization-assistant`.
