# How to use - Codebase Health Dashboard

**What it is.** The "run everything" tool. Point it at your repo - .NET, Node.js/TypeScript, Python, Java/Kotlin, Go, Rust, or a polyglot monorepo - and it detects the stack, collects hard signals with your stack's own tools (build, tests, coverage, outdated dependencies, known vulnerabilities), runs the toolkit's auditors, scores eight categories with a fixed rubric, and writes a single self-contained **health dashboard** - one overall score, per-category scores, the findings behind each, and the top fixes.

**When to reach for it.** A whole-repo health check, an overall code-quality score, a pre-release report card, or to track your codebase score week to week.

**How to use it.** Point it at your project folder or a GitHub URL. Example prompts:
- "Run a health check on this repo."
- "Give my .NET solution an overall score and a report card."
- "How healthy is this TypeScript monorepo? Show me where I stand."
- "Score my Spring Boot service, including dependency vulnerabilities."
- "Health check this Go repo - run the tests and govulncheck too."

**What you get.** `health-dashboard/index.html` (overall score, detected stacks, measured signals, eight category cards with findings, top 3 actions) and a `health/health-<date>.json` record for tracking the trend.

**Get the best out of it.**
- **Run it where the project builds** (SDKs installed, dependencies restorable) so build/test/coverage/vuln signals are measured instead of marked "not measured".
- **Re-run it weekly** and watch the score climb - that is the point. Each run saves a record in `health/`.
- Act on the **Top actions** first - they are the fastest wins.
- The scoring is **deterministic** (critical -25, high -12, medium -5, low -2 per category, from 100), so the number is comparable run to run.
- Share your score (or a screenshot) in the community - it is great for the "show your results" thread.

**Won't do.** It won't invent findings to lower your score - it credits what is already healthy. It won't guess results it couldn't measure; index/CVE claims that need a live DB or a full restore are marked "verify" or "not measured", same as the underlying tools.
