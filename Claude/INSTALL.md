# Install & Use - Claude Code Starter Skills for Developers

Works with any stack: C#/.NET, TypeScript/Node, Python, Java/Kotlin, Go, and more.

## Option 1 - plugin marketplace (recommended)

```shell
/plugin marketplace add StefanTheCode/dotnet-claude-starter
/plugin install claude-starter@thecodeman-claude-starter
```

Update anytime:

```shell
/plugin marketplace update thecodeman-claude-starter
```

## Option 2 - manual copy

Three folders matter: `skills/`, `agents/`, and `templates/`.

### Install the skills

Copy the skill folders into a `skills/` directory Claude Code reads:

```bash
# user-level - available in every project (recommended)
mkdir -p ~/.claude/skills
cp -r skills/* ~/.claude/skills/

# or project-level - just this project
mkdir -p .claude/skills
cp -r skills/* .claude/skills/
```

### Install the agent

```bash
mkdir -p ~/.claude/agents
cp agents/api-security-auditor.md ~/.claude/agents/
```

### Add a CLAUDE.md

Copy `templates/CLAUDE-md-template.md` (the generic, stack-agnostic template) to the **root of your repository**, rename it to `CLAUDE.md`, and fill in the blanks - instructions are at the top of the file. Want a head start? Use the filled-in example for your stack instead: `dotnet-CLAUDE-md-template.md`, `typescript-CLAUDE-md-template.md`, or `python-CLAUDE-md-template.md`. Claude Code loads `CLAUDE.md` automatically at the start of every session.

---

## Use

Reopen your Claude Code session, then just describe what you want:

```
> This EF query is slow, optimize it
> This Prisma query is slow - check src/orders
> Review this asyncio code for blocking calls
> Find floating promises in this Node service
> Scaffold a clean architecture project in Spring Boot
> Benchmark these two functions
> What's not tested in this project?
> Audit the security of this API      <- runs the security agent
```

Skills detect your stack from the repo and trigger automatically when your request matches; the agent runs when you ask for what it does.

---

## Verifying a skill works

1. Install it (above) and reopen the session.
2. Use a prompt from the skill's `USAGE.md` examples.
3. Confirm Claude applies the skill's checklist/format rather than a generic answer.

If a skill doesn't trigger, the fix is almost always the **description** in its `SKILL.md` frontmatter - make it specific about when to fire.

---

## Want the full toolkit?

This is 5 skills + 1 agent. The full set - **49 skills and 7 agents** - lives inside the **[AI for Developers community on Skool](https://www.skool.com/ai-for-dotnet-developers/about)** (7-day free trial), with Claude Code & agent workflow lessons, building AI features, and live office hours on top.

📬 Weekly newsletter on .NET, architecture & AI (21,000+ developers): [thecodeman.net](https://thecodeman.net) · ▶️ [YouTube](https://www.youtube.com/@thecodeman_)
