# How to use - Legacy Modernization Assistant (agent)

**What it is.** An agent that assesses a legacy codebase in any stack and produces a pragmatic, incremental modernization roadmap (strangler fig, not big-bang) - platform, architecture, DI, async, typing, testability - sequenced by risk and value. It knows the common legacy shapes per stack: **.NET Framework** (WebForms, WCF, packages.config, ADO.NET, WinForms), untyped or callback-era **JavaScript**, old **Python**/Django, J2EE or XML-configured **Java**/Spring, and pre-modules **Go**.

**When to reach for it.** Modernizing legacy code, migrating off an old platform, or "where do I even start with this old codebase".

**How to use it.** Point it at the codebase (path or GitHub URL). Example prompts:
- "Assess this legacy app and give me a modernization roadmap."
- "We're on .NET Framework - how do we move to modern .NET incrementally?"
- "This Express app is plain JS with callbacks everywhere. Plan a gradual move to TypeScript."
- "Our Spring app still deploys a WAR with XML config. How do we get to Spring Boot 3 safely?"
- "Where do I start untangling this Django monolith?"

**Get the best out of it.** Decide the destination with it first - consolidate in place (for .NET: stay on **.NET Framework 4.8** with modern tooling) or port to the current platform (for .NET: **modern .NET**) - because that changes which blockers matter. It runs a concrete blocker scan for your stack and hands the actual edits to the right skill or agent. For .NET Framework systems that means the .NET pack (`packs/dotnet`): `legacy-package-migrator` (packages.config -> Central Package Management), `adonet-data-access-auditor` (classic ADO.NET), `winforms-maintainer` (WinForms fixes). Across stacks it uses `framework-upgrade-agent`, `modular-monolith-generator`, `test-coverage-gap-finder`, and `async-await-auditor`.

**Try it on the sample.** `packs/dotnet/examples/legacy-pack/` ships a deliberately broken .NET Framework solution - point the agent at it and compare against its `VALIDATION-REPORT.md`.

**Won't do.** It won't recommend a big-bang rewrite unless there's strong justification - incremental is the default. It plans and sequences; the skills make the edits, one project or module per PR.
