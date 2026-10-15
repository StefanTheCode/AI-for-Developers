# Install & Use

## Option A - Claude Code plugin (recommended)

```shell
/plugin marketplace add StefanTheCode/AI-for-Developers
/plugin install ai-dev-toolkit@thecodeman-ai-toolkit
# optional, .NET only:
/plugin install dotnet-pack@thecodeman-ai-toolkit
```

Skills trigger automatically when your request matches; invoke an agent by asking for what it does ("audit the security of this API"). Pull updates anytime:

```shell
/plugin marketplace update thecodeman-ai-toolkit
```

## Migrating from dotnet-ai-toolkit

The old `dotnet-ai-toolkit` repo and plugin are replaced by this one. Run once:

```shell
/plugin uninstall dotnet-ai-toolkit@thecodeman-ai-toolkit
/plugin marketplace remove thecodeman-ai-toolkit
/plugin marketplace add StefanTheCode/AI-for-Developers
/plugin install ai-dev-toolkit@thecodeman-ai-toolkit
/plugin install dotnet-pack@thecodeman-ai-toolkit
```

Renamed skills and agents (same job, now for any stack):

| Old | New |
|---|---|
| ef-core-query-optimizer | orm-query-optimizer |
| ef-index-advisor | db-index-advisor |
| ef-migration-reviewer | db-migration-reviewer |
| dbcontext-config-auditor | orm-config-auditor |
| sql-linq-converter | sql-orm-converter |
| minimal-api-endpoint-scaffolder | api-endpoint-scaffolder |
| cqrs-mediatr-setup | cqrs-setup |
| netarchtest-generator | architecture-test-generator |
| authorization-policy-generator | authorization-setup |
| options-pattern-generator | typed-config-generator |
| nuget-dependency-analyzer | dependency-analyzer |
| serilog-logging-setup | structured-logging-setup |
| stj-serialization-optimizer | serialization-optimizer |
| xunit-test-generator | unit-test-generator |
| benchmarkdotnet-setup | benchmark-setup |
| aspnetcore-security-auditor (agent) | api-security-auditor |
| dotnet-architecture-reviewer (agent) | architecture-reviewer |
| dotnet-code-reviewer (agent) | code-reviewer |
| dotnet-upgrade-agent (agent) | framework-upgrade-agent |

Moved to the .NET Pack: span-memory-refactor, legacy-package-migrator, adonet-data-access-auditor, winforms-maintainer.

## Option B - Manual (single skill or agent)

```bash
# skill, project-level
mkdir -p .claude/skills && cp -r toolkit/skills/orm-query-optimizer .claude/skills/
# skill, user-level (every project)
mkdir -p ~/.claude/skills && cp -r toolkit/skills/orm-query-optimizer ~/.claude/skills/
# agent
mkdir -p ~/.claude/agents && cp toolkit/agents/code-reviewer.md ~/.claude/agents/
```

Reopen the session afterwards.

## Add a CLAUDE.md

Copy `toolkit/templates/CLAUDE-md-template.md` to your repo root as `CLAUDE.md` and fill in the blanks. Filled examples: `dotnet-`, `typescript-` and `python-CLAUDE-md-template.md`.

## Verifying a skill works

1. Install it and start a fresh session.
2. Use a prompt from the skill's `USAGE.md`.
3. Confirm Claude follows the skill's checklist and output format instead of a generic answer.

If a skill doesn't trigger, the fix is almost always the `description` in its `SKILL.md` frontmatter - make it specific about when to fire.
