---
name: migration-assessment-report
description: (.NET) Assess a .NET Framework solution (WebForms, MVC 5, Web API 2, WCF, EF6, class libraries) for a move to .NET 10 and produce a self-contained HTML report - architecture diagrams, a blocker scan, a target per WebForms page (Razor Pages, Blazor or MVC), effort in person-days with and without AI, and a clear split of what an AI agent can do versus what needs a human. Use this skill whenever the user asks "how hard is it to move this to .NET 10", "assess this legacy solution", "give me a migration estimate", "what would it take to modernize this", "should we migrate this WebForms app", or wants a migration report for a manager or a client. Always use this skill for migration assessments instead of answering from memory; it runs a deterministic scanner, reviews the result and renders the report.
category: Legacy (.NET)
---

# Migration Assessment Report (beta)

Turn an old .NET Framework solution into one HTML file a team lead, a manager or a client can read in ten minutes: what's in the code, what blocks a move to .NET 10, how big the job is, what an AI agent can take over and what stays human work.

Two parts, on purpose:

1. **`scripts/scan.cs` counts.** Projects, pages, controllers, WCF operations, EF contexts, packages, blocker signals, and a first estimate per unit of work. Deterministic - same code, same numbers.
2. **You judge.** Read the hot spots, check every WebForms target, write the verdict, risks, questions and next steps into the JSON. Then `scripts/render.cs` draws the report.

The report is the input for the `dotnet-migration-orchestrator` agent and every migration skill in this pack.

## Input - point it at the solution

Find the `.sln` / `.slnx` yourself with Glob instead of asking for a paste. If there are several, ask which one. You only need read access to the source; nothing is built or run.

## Requirements

- **.NET 10 SDK** on the machine (`dotnet --version` shows 10.x). The scripts are .NET 10 file-based apps - no project, no NuGet packages, no network.
- If the SDK is missing, say so and stop. Don't guess the inventory by hand - the numbers would not be reproducible.

## Process

### 1. Scan

```bash
dotnet run <this-skill-folder>/scripts/scan.cs -- <path/to/Solution.sln>
```

It writes `migration/migration-assessment.json` next to the solution and prints a short summary (units per kind, first estimate). Use the real path of this skill's folder. The first run compiles the script, which takes a few seconds.

### 2. Read before you judge

Open the JSON, then read the actual code behind the biggest items - don't write the verdict from the counts alone:

- every `L` unit, and every page whose target is Blazor
- every file listed under the `hard` blockers
- `Global.asax.cs`, `Web.config` (auth, session, machineKey, `<location>` rules, WCF endpoints)
- the class that holds pricing / business rules, if there is one (quirks live there)

### 3. Enrich the JSON

Edit `migration/migration-assessment.json` directly. Only these fields are yours - the schema is in `references/report-schema.md`:

| Field | What to write |
|---|---|
| `summary.verdict` | `go`, `go-with-conditions` or `not-now` |
| `summary.headline` | One plain sentence a manager understands |
| `summary.destination` | The path: consolidate on 4.8.1, or port to .NET 10 incrementally (strangler fig), and the key technology choices |
| `summary.verdictReason` | 2-4 sentences. Name the hard blockers and why they are (or aren't) a problem |
| `units[].target` / `targetReason` | Check every WebForms target with `references/webforms-target-rules.md`. Change it when the code says otherwise, and say why in one concrete sentence |
| `units[].size` / `hoursLow` / `hoursHigh` | Change only with a reason you add to `notes` (e.g. "code-behind is 900 lines with 14 event handlers") |
| `units[].humanTasks` | Decisions only a person can make for that unit |
| `risks[]` | 4-8 items, `title` + `impact` (`high`/`medium`/`low`) + `mitigation`. Behavior risks first (quirks customers see), then auth/session, then infrastructure |
| `questions[]` | What the code can't tell you: API consumers, hosting, which quirks are bugs |
| `nextSteps[]` | 3-6 concrete steps for the first two weeks, in order |

Rules:

- **Don't tune the numbers to sound good.** If the scan says 200 hours, the report says 200 hours. Change the effort model (`effortModel`) only when the user gives real team data (hours per day, experience with the code).
- **Every claim needs a file.** "Session is used for the cart" -> name the class.
- **Quirks are risks.** Business behavior that looks like a bug (rounding, totals that don't match, prices kept from page load) goes into `risks` and `questions`, never into "we'll fix it during the move".
- **Plain language.** The report is read by people who don't know what ViewState is. Explain it in the same sentence.

### 4. Render

```bash
dotnet run <this-skill-folder>/scripts/render.cs -- migration/migration-assessment.json
```

It recomputes every total from the JSON, so your edits are reflected, and writes `migration/migration-assessment.html` (about 2 MB - Mermaid is embedded so the diagrams work offline and as an email attachment).

### 5. Check and report back

- Confirm the HTML exists and print its size.
- If a browser tool is available, open the file and check the diagrams rendered.
- Tell the user, in this order: the verdict, the size and the day ranges (without AI / with AI), the top 3 risks, the questions they need to answer, and the path to the HTML.

## What's in the report

- KPIs: size (S/M/L/XL), person-days without AI and with an AI agent, the share of hands-on work an agent can take over, hard blockers
- Executive summary with the recommended path and top risks
- Where the effort goes (by kind of work) and who does what (AI vs developer)
- Diagrams: architecture today, target architecture, the strangler-fig move with YARP, a roadmap (Gantt)
- WebForms: target per page with the reason
- Every unit of work with hours without and with AI
- Blockers with severity, meaning, fix and file locations
- Projects and packages (keep / bridge / replace / remove / check)
- Risks, questions for the team, next steps, and how the numbers were made

## Rules

- Write only inside `migration/`. Don't touch source code.
- Don't run the app, the build or the tests - this is a read-only assessment.
- The estimate is a range with visible assumptions. Never present it as a quote.
- If the user wants their own branding, edit `assets/report-template.html` (`--accent` and the footer).

## Works in GitHub Copilot too

Same skill file, same scripts. Copilot asks before it runs `dotnet run` - approve both commands.
