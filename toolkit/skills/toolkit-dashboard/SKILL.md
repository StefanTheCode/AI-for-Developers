---
name: toolkit-dashboard
description: Build, refresh, and OPEN the TheCodeMan AI for Developers Toolkit dashboard - a self-contained web page that catalogs every skill and agent (core toolkit plus stack packs like the .NET pack) and shows past run results. Use this skill FIRST when opening the toolkit, or whenever the user wants an overview of skills/agents, a dashboard, a home page, run results/history, a case-study showcase, the prompt pack, or says "show me the toolkit / open the dashboard / refresh the dashboard / run the dashboard / what skills do I have". When triggered, Claude runs the generator itself with the Bash tool and then opens/presents the dashboard for the user - the user never has to run Python manually. Always use this skill for toolkit overview and dashboard requests.
category: Meta / Tooling
version: 1.0.0
---

# Toolkit Dashboard

Generate a single, self-contained `toolkit/dashboard/index.html` that gives a clear visual overview of the whole toolkit: every skill and agent (name, category, pack, version, description, docs), plus a history of skill/agent runs and their findings. It runs on localhost with one command and has zero runtime dependencies. The toolkit is stack-neutral; the dashboard simply catalogs whatever is in the repo.

## When this runs

- **First thing** when someone opens the toolkit and wants to see what's inside.
- Whenever a skill or agent is **added, updated, or run** - regenerate so the dashboard stays current.
- When the user asks for an overview, a dashboard, run history, or "what do I have".

## Repo layout it scans

```
<repo>/
  toolkit/
    skills/<name>/SKILL.md          core skills (any stack)
    agents/<name>.md                core agents (usage notes in agent-usage/<name>.USAGE.md)
    runs/*.json                     run records (input)
    catalog-meta.json               optional metadata fallback (input)
    dashboard/index.html            generated
    showcase/index.html             generated (optional)
    dist/...Prompt-Pack.pdf         generated (optional)
  packs/
    <pack>/skills/<name>/SKILL.md   stack-specific packs, e.g. packs/dotnet
    <pack>/agents/<name>.md         optional
```
The scripts live in `toolkit/skills/toolkit-dashboard/scripts/` and auto-detect the repo from their own location. `--root` accepts either the repo root or the `toolkit/` folder. The old flat layout (`skills/` and `agents/` at the root) still works.

## What it does

1. **Scans `toolkit/skills/*/SKILL.md` and `packs/*/skills/*/SKILL.md`** - parses YAML frontmatter (`name`, `description`, optional `category`, `version`), records which pack it came from (`core` or the pack folder name, shown as a "dotnet pack" pill), and notes whether each skill has a `USAGE.md` and a `references/` folder.
2. **Scans `toolkit/agents/*.md` and `packs/*/agents/*.md`** (excluding `*.USAGE.md`) - parses frontmatter (`name`, `description`, `tools`, optional `category`, `version`).
3. **Merges `toolkit/catalog-meta.json`** (optional) - fills in `category`/`version`/`tags` for any item that doesn't declare them in frontmatter.
4. **Loads `toolkit/runs/*.json`** - structured run records (see the schema below) and renders them as a searchable history with verdict badges and findings.
5. **Writes `toolkit/dashboard/index.html`** - one file, all data embedded, no external requests. Works over `file://` or a local server.

## How Claude runs it (no terminal for the user)

When this skill triggers, **Claude builds and opens the dashboard for the user** - they should never have to run Python themselves.

1. **Build it** with the Bash tool. No arguments needed:
   ```bash
   python3 "<repo>/toolkit/skills/toolkit-dashboard/scripts/build_dashboard.py"
   ```
   (`--root <repo>` also works if you want to be explicit.) The console prints the counts and which folders were scanned - check that every pack you expect is listed.
2. **Open it for the user:**
   - **Cowork:** present `toolkit/dashboard/index.html` with the file-presentation tool so they open it in one click.
   - **Claude Code / terminal:** give them the path and offer to serve it - `cd toolkit/dashboard && python3 -m http.server 8080` -> `http://localhost:8080`.
3. **(Optional) Case-study showcase:**
   ```bash
   python3 "<repo>/toolkit/skills/toolkit-dashboard/scripts/build_showcase.py" --target "Project Atlas" --stack ".NET 10"
   ```
   -> `toolkit/showcase/index.html` (a branded, screenshot-ready results page). Pass the stack of the target codebase, or `--stack ""` to omit it.
4. **(Optional) Prompt pack PDF** (needs `reportlab`): `python3 .../scripts/build_prompt_pack.py` -> `toolkit/dist/TheCodeMan-AI-Toolkit-Prompt-Pack.pdf`.
5. **(Optional) Demo data:** `python3 .../scripts/seed_showcase_runs.py` writes the anonymized "Project Atlas" run records into `toolkit/runs/`.

The page is a single self-contained file (data embedded, zero dependencies), so opening `toolkit/dashboard/index.html` directly via `file://` always works. Re-running is cheap and idempotent - just rebuild after any change.

## The `runs/` convention (how results show up)

For a skill or agent run to appear in the dashboard, drop a JSON file in `toolkit/runs/` following the schema in `references/run-record-schema.md`. Minimal example:

```json
{
  "id": "2026-06-16-orm-query-optimizer-atlas",
  "tool": "orm-query-optimizer",
  "type": "skill",
  "target": "Project Atlas",
  "date": "2026-06-16",
  "verdict": "issues",
  "summary": "In-memory filtering + missing AsNoTracking in Configurations/Queries/List.cs",
  "findings": [
    { "severity": "medium", "title": "No projection / over-fetch", "location": "Configurations/Queries/List.cs" },
    { "severity": "medium", "title": "Missing AsNoTracking", "location": "Configurations/Queries/List.cs" }
  ],
  "report": "TEST-RESULTS.md"
}
```

**Verdict values** (drive the badge color): `clean`, `pass`, `issues`, `critical`, `generated`.
**Severity values:** `low`, `medium`, `high`, `critical`.
**`tool`** must equal the skill/agent frontmatter `name` (current names, e.g. `orm-query-optimizer`, `api-security-auditor`) so the run links to its catalog entry and category.

When you run any other skill/agent and want it recorded, write a matching record to `toolkit/runs/` and re-run the generator. The dashboard updates automatically.

## Output

- `toolkit/dashboard/index.html` - the dashboard (overwritten each run).
- Console prints counts (skills, agents, categories, runs), the scanned folders, and the launch command.

## Principles

- **One file, no dependencies.** Members can open it anywhere; it never phones home.
- **Self-describing.** Categories/versions come from the skills themselves (frontmatter) first, `catalog-meta.json` second - no hardcoded list to maintain. New packs under `packs/` are picked up automatically.
- **Re-run cheap.** Regenerating is idempotent; run it after every change.
- **On brand.** Deep purple-navy background, yellow accents, dark cards - matches TheCodeMan visual identity.
