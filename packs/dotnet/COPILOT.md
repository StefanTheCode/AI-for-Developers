# Legacy Pack with GitHub Copilot

The Legacy Pack works with GitHub Copilot as well as with Claude Code. Use this guide for Visual Studio 2026, VS Code or Copilot CLI.

## What's in the pack

| Type | Name | What it does |
|---|---|---|
| Skill | `legacy-package-migrator` | `packages.config` → `PackageReference` → Central Package Management, no version drift |
| Skill | `adonet-data-access-auditor` | SQL injection, leaked connections/readers, missing transactions in classic ADO.NET |
| Skill | `winforms-maintainer` | Cross-thread errors, frozen UI, leaks, `DataGridView` issues. Small fixes, no rewrite |
| Skill | `span-memory-refactor` | `Span<T>` / `Memory<T>` refactors for hot parsing code |
| Agent | `legacy-modernization-assistant` | Assesses a .NET Framework codebase and gives you a staged modernization roadmap |
| Agent | `framework-upgrade-agent` | Plans and runs the version bumps (.NET Framework → modern .NET, TFM upgrades) |

- **Skills:** the same `SKILL.md` files Claude Code uses. Copilot reads them without changes.
- **Agents:** they have Copilot editions in `copilot/agents/*.agent.md`. The instructions are the same, only the header differs, because Copilot agents use their own tool names.

## Requirements

- **Visual Studio:** Visual Studio 2026 **18.5 or later**, in **Agent** mode (not Ask).
- **VS Code:** current version, Copilot Chat in **Agent** mode.
- **Copilot CLI:** works on every Copilot plan.

---

## Option 1: Put it in your repo (works everywhere, recommended)

This works in Visual Studio, VS Code, Copilot CLI and the Copilot cloud agent. Commit the `.github` folder and your whole team gets it too.

**PowerShell**

```powershell
git clone https://github.com/StefanTheCode/AI-for-Developers
$pack = ".\AI-for-Developers\packs\dotnet"
$repo = "C:\path\to\your-legacy-solution"   # <- change this

New-Item -ItemType Directory -Force "$repo\.github\skills", "$repo\.github\agents" | Out-Null
Copy-Item "$pack\skills\*" "$repo\.github\skills\" -Recurse -Force
Copy-Item "$pack\copilot\agents\*.agent.md" "$repo\.github\agents\" -Force
```

**bash**

```bash
git clone https://github.com/StefanTheCode/AI-for-Developers
pack=AI-for-Developers/packs/dotnet
repo=/path/to/your-legacy-solution   # <- change this

mkdir -p "$repo/.github/skills" "$repo/.github/agents"
cp -r "$pack/skills/." "$repo/.github/skills/"
cp "$pack"/copilot/agents/*.agent.md "$repo/.github/agents/"
```

When you're done, your repo should look like this:

```text
your-legacy-solution/
└── .github/
    ├── skills/
    │   ├── adonet-data-access-auditor/SKILL.md
    │   ├── legacy-package-migrator/SKILL.md
    │   ├── span-memory-refactor/SKILL.md
    │   └── winforms-maintainer/SKILL.md
    └── agents/
        ├── framework-upgrade-agent.agent.md
        └── legacy-modernization-assistant.agent.md
```

Reopen the solution, or reload the VS Code window. If you start a new Copilot CLI session, it picks them up on its own.

> Already using Claude Code in this repo? Copilot also reads `.claude/skills/`, so skills you have there show up in Copilot automatically.

## Option 2: Copilot CLI plugin (skills only)

The pack's marketplace also works in Copilot CLI:

```shell
copilot plugin marketplace add StefanTheCode/AI-for-Developers
copilot plugin install dotnet-pack@thecodeman-ai-toolkit
```

This installs the 4 legacy skills. The plugin agents are in Claude Code format, so for the two agents copy the `.agent.md` files to `~/.copilot/agents/` (or `.github/agents/` in your repo):

```powershell
New-Item -ItemType Directory -Force "$HOME\.copilot\agents" | Out-Null
Copy-Item ".\AI-for-Developers\packs\dotnet\copilot\agents\*.agent.md" "$HOME\.copilot\agents\" -Force
```

Want the rest of the toolkit too (45 skills for any stack)? Run `copilot plugin install ai-dev-toolkit@thecodeman-ai-toolkit`. Get updates later with `copilot plugin update dotnet-pack`.

## Option 3: Personal install (every project on your machine)

| What | Visual Studio | VS Code | Copilot CLI |
|---|---|---|---|
| Skills | `~\.copilot\skills\` | `~/.copilot/skills/` | `~/.copilot/skills/` or the plugin |
| Agents | `%USERPROFILE%\.github\agents\` | *Chat: New Custom Agent* → User profile | `~/.copilot/agents/` |

In Visual Studio you can change the agent folder in **Tools > Options > GitHub > Copilot > Copilot Chat > Custom agents user directory**.

---

## How to use it

**Skills** load on their own in Agent mode when your request matches. Describe the job as you normally would, e.g. "audit this OrderRepository for SQL injection and leaks". In VS Code you can also call one directly: type `/legacy-package-migrator` in chat.

**Agents** work in a few ways:
- **Visual Studio / VS Code:** pick `legacy-modernization-assistant` in the agent picker in Copilot Chat.
- **Copilot CLI (interactive):** type `/agent` and choose it.
- **Copilot CLI (one-shot):** `copilot --agent=legacy-modernization-assistant --prompt "Assess this solution and give me a staged migration plan"`

Agents use whichever model you have selected. For the modernization assessment, pick a strong reasoning model. It's a long, multi-step job.

## Try it on the sample first

The pack includes a small .NET Framework solution with **19 planted defects**. Set it up in a throwaway folder, so nothing touches your real code:

```powershell
$pack = ".\AI-for-Developers\packs\dotnet"
$demo = "C:\temp\legacy-pack-demo"

New-Item -ItemType Directory -Force "$demo\.github\skills", "$demo\.github\agents" | Out-Null
Copy-Item "$pack\skills\*" "$demo\.github\skills\" -Recurse -Force
Copy-Item "$pack\copilot\agents\*.agent.md" "$demo\.github\agents\" -Force
Copy-Item "$pack\examples\legacy-pack\sample-legacy-erp\*" "$demo\" -Recurse -Force
Copy-Item "$pack\examples\legacy-pack\VALIDATION-REPORT.md" "$demo\" -Force
git -C $demo init
```

Open `C:\temp\legacy-pack-demo` in VS Code, open `LegacyErp.sln` in Visual Studio, or `cd` into it for Copilot CLI. Then run these prompts one by one:

```text
Migrate this solution to Central Package Management and unify versions.
Audit LegacyErp.Data/OrderRepository.cs for SQL injection and leaks.
Fix the cross-thread and leak issues in LegacyErp.UI/MainForm.cs.
```

For the last step, pick the `legacy-modernization-assistant` agent and run:

```text
Assess this solution and give me a staged migration plan.
```

The sample is meant to be broken, so it won't build. Compare what Copilot finds with `VALIDATION-REPORT.md`. The 19/19 baseline was measured with Claude Code. In Copilot, results depend on the model you pick.

## Troubleshooting

- **A skill doesn't show up:** the path must be `.github/skills/<skill-name>/SKILL.md`, and the folder name must match `name:` in the file. In Copilot CLI, run `/skills reload`.
- **An agent isn't in the picker:** the file must be in `.github/agents/` and end with `.agent.md`. Reopen the solution or reload the window.
- **Visual Studio ignores everything:** check that you're on 18.5+ and in **Agent** mode.
- **Copilot asks before running commands:** that's normal. The agents run `dotnet build` / `dotnet test` and code searches, and Copilot asks for approval. Approve what you're comfortable with.

Questions or something not working? Post it in the [AI for Developers community](https://www.skool.com/ai-for-developers/about).
