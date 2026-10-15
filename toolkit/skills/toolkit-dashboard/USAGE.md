# How to use - Toolkit Dashboard

**What it is.** A "home" skill that generates a single self-contained `toolkit/dashboard/index.html` - a localhost web page that catalogs every skill and agent in the AI for Developers Toolkit (the stack-neutral core in `toolkit/` plus stack packs such as `packs/dotnet`) and shows the history of skill/agent runs with their findings. Zero dependencies, one file, runs anywhere.

**When to reach for it.** Run it **first** when you open the toolkit, and again any time you add, update, or run a skill/agent so the overview stays current.

**How to use it - just ask Claude.** You don't run anything yourself. Say "show me the dashboard" / "open the toolkit" / "refresh the dashboard" and Claude runs the generator (Bash) and opens the dashboard for you (presents it in Cowork, or serves it in Claude Code).

Example prompts that trigger it:
- "Open the dashboard."
- "What skills do I have for Python / for .NET?"
- "Refresh the dashboard - I just added a skill to the .NET pack."
- "I want to see the run results."
- "Build the case-study showcase for our Node.js service."

Manual fallback (only if you want a terminal): `python3 toolkit/skills/toolkit-dashboard/scripts/build_dashboard.py` from the repo root (auto-detects the layout - no `--root` needed), then open `toolkit/dashboard/index.html` or serve it with `python3 -m http.server 8080`.

**What you get.** A dashboard with Skills, Agents, and Run results tabs; search and category filter; a pack pill on pack items (e.g. "dotnet pack"); USAGE/references markers per skill. Optional extras: a screenshot-ready showcase page, a prompt-pack PDF, and demo run data.

**How to get the best out of it.**
- **Record your runs.** After running any skill/agent, drop a JSON record in `toolkit/runs/` (schema in `references/run-record-schema.md`) and re-run the generator - that's how results appear in the history. Use the tool's current `name` so it links correctly.
- **Let skills self-describe.** Add `category:` and `version:` to a skill's frontmatter and the dashboard picks them up automatically. `toolkit/catalog-meta.json` is the fallback for items that don't declare them.
- **Add a pack by adding a folder.** `packs/<stack>/skills/<name>/SKILL.md` shows up on the next build - no config.
- **Re-run freely.** It's idempotent - regenerating just overwrites the dashboard.

**What it won't do.** It doesn't *execute* skills - it catalogs them and displays results you record. It serves on localhost (or `file://`); it's not a hosted web app. It reads only the repo folders, so anything you want shown (a run, a category) has to exist as a file.
