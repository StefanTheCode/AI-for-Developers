# .NET Pack

Optional add-on for the AI for Developers Toolkit. The core toolkit (`toolkit/skills`, `toolkit/agents`) works in any stack and already covers .NET as a first-class stack. This pack holds the skills that only make sense in .NET: low-level C# performance work, maintenance of legacy .NET Framework systems and, new, the **Migration Pack (beta)** that moves a .NET Framework app to .NET 10 piece by piece.

## Install

```
/plugin install dotnet-pack@thecodeman-ai-toolkit
```

### Using GitHub Copilot?

The skills work in Copilot as they are, and every agent has a Copilot edition in `copilot/agents/`. Setup for Visual Studio 2026, VS Code and Copilot CLI is in [COPILOT.md](COPILOT.md).

## Legacy Pack skills

| Skill | What it does |
|---|---|
| `span-memory-refactor` | Refactors hot C# parsing, slicing and buffer code to `Span<T>` / `ReadOnlySpan<T>` / `Memory<T>` to cut allocations, and flags where spans don't fit. |
| `legacy-package-migrator` | Moves a .NET Framework solution from `packages.config` to `PackageReference`, then to Central Package Management (`Directory.Packages.props`), removing version drift across projects. |
| `adonet-data-access-auditor` | Audits classic ADO.NET (`SqlConnection` / `SqlCommand` / `SqlDataReader` / `DataSet`) for SQL injection, leaked connections and readers, repeated reads and missing transactions, and rewrites the code. |
| `winforms-maintainer` | Fixes cross-thread errors, frozen UI, disposal and event-handler leaks, and `DataGridView` / `BindingSource` issues in existing WinForms apps with minimal, behavior-preserving changes. No UI rewrite. |

These skills pair with the core `legacy-modernization-assistant` agent, which assesses a .NET Framework codebase, picks the destination (consolidate on .NET Framework 4.8 or port to modern .NET) and hands the concrete edits to the skills above, and with `framework-upgrade-agent` for the version bumps.

## Migration Pack (beta): .NET Framework to .NET 10

No big-bang rewrite. A new .NET 10 app goes in front of the old one (YARP + System.Web adapters), and the system moves over one page, controller, service or DbContext at a time. Every slice is proven by parity tests before its route switches, and the app keeps working the whole time.

It starts with a report: an HTML page with diagrams that shows what has to change, how big the job is, a day range with and without AI, and which parts AI can do and which need a person.

| Skill / agent | What it does |
|---|---|
| `migration-assessment-report` | Scans the solution (WebForms, MVC 5, Web API 2, WCF, EF6, `System.Web` use, packages) and writes `migration/migration-assessment.json` + a self-contained HTML report: diagrams of today and the target, the strangler plan, effort with and without AI, a target per WebForms page, blockers, risks and questions. |
| `migration-safety-net` | Records the old app's answers as parity baselines (normalized HTML/JSON), plus Playwright flow tests and characterization tests for business logic. The new app has to match them. |
| `yarp-strangler-setup` | Creates the .NET 10 host: YARP fallback to the old app, remote session and remote authentication through `Microsoft.AspNetCore.SystemWebAdapters`, shared config. |
| `webforms-page-migrator` | Moves one WebForms page to the target the report picked (Razor Pages, Blazor Server, a layout or a view component), same URL, same behavior. |
| `aspnet-mvc-webapi-migrator` | Moves MVC 5 and Web API 2 controllers to ASP.NET Core with the same routes, status codes and JSON shape. |
| `wcf-service-migrator` | Hosts a WCF service in CoreWCF on .NET 10 with the same contract, namespace and faults, so existing clients don't change. Contract tests included. |
| `ef6-to-efcore-migrator` | The data layer in two steps: EF 6.5 on .NET 10 first (no query changes), then EF Core context by context, with query-parity tests. |
| `legacy-config-auth-migrator` | `web.config` to `appsettings`, Forms auth and machineKey to cookie auth and Data Protection, session, BinaryFormatter and error handling. |
| Agent `dotnet-migration-orchestrator` | Runs the whole thing: assessment, the human decisions, safety net, new host, then slice by slice. Keeps the state in `migration/MIGRATION.md`, so you can stop and continue in any session or tool, and stops for a person at every decision gate. |

**How to start:**

1. "Assess `<YourSolution>.sln` for migration to .NET 10" - open `migration/migration-assessment.html`.
2. Read the report, answer the questions in it.
3. Pick the `dotnet-migration-orchestrator` agent: "Migrate this solution to .NET 10, start with phase 1". Next day: "Continue the migration".

Work on a branch. The agents change files and commit one slice at a time.

**Requirements:** .NET 10 SDK (the scanner and the report renderer are `dotnet run file.cs` scripts, no install). The old app still needs Windows and Visual Studio to build and run.

**Beta:** the scanner, the report and the code in the skills were tested on `examples/legacy-shop`. Your app will have things it hasn't seen - tell me in the community what it missed.

### Example: legacy-shop

`examples/legacy-shop` is a small web shop on .NET Framework 4.8 that **builds and runs**: 6 WebForms pages with a master page, a user control and an `.ashx` handler, Web API 2, an MVC 5 report, a WCF service, EF6 with LocalDB and a shared library that uses `HttpContext.Current`. `TRAPS.md` lists the 29 migration traps and 9 behavior quirks planted in it. `verify.ps1` builds it, starts it and runs 17 smoke checks. `migration/migration-assessment.html` is the report the pack generated for it.

## Example: legacy-pack

`examples/legacy-pack/sample-legacy-erp` is a small, deliberately broken .NET Framework solution with **19 planted defects** across a classic ADO.NET data layer, a WinForms form, and two `packages.config` files with real version drift. Point each skill (and the `legacy-modernization-assistant` agent) at it and compare the output with `examples/legacy-pack/VALIDATION-REPORT.md`. Baseline: 19/19 detected.

It's a teaching and validation fixture, not a runnable app. See `examples/legacy-pack/README.md` for ready-to-use prompts.
