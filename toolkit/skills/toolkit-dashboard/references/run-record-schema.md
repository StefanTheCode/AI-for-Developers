# Run Record Schema (`runs/*.json`)

Each file in `toolkit/runs/` describes one execution of a skill or agent. The dashboard reads every `*.json` here and renders the History tab. One record per file; filename is free (convention: `<date>-<tool>-<target>.json`).

## Fields

| Field | Required | Type | Notes |
|-------|----------|------|-------|
| `id` | yes | string | Unique id, e.g. `2026-06-16-orm-query-optimizer-atlas`. |
| `tool` | yes | string | The `name` of the skill/agent that ran (must match its frontmatter name to link). |
| `type` | yes | `"skill"` \| `"agent"` | What ran. |
| `target` | yes | string | What it ran against (repo/file/project), e.g. `Project Atlas`. |
| `date` | yes | string | ISO date `YYYY-MM-DD`. |
| `verdict` | yes | enum | `clean` \| `pass` \| `issues` \| `critical` \| `generated`. Drives badge color. |
| `summary` | yes | string | One-line headline result. |
| `findings` | no | array | List of `{ severity, title, location }`. |
| `report` | no | string | Path to a fuller report (e.g. `TEST-RESULTS.md`), shown as a link. |
| `duration_s` | no | number | Optional run duration in seconds. |

### Finding object

| Field | Required | Type | Notes |
|-------|----------|------|-------|
| `severity` | yes | enum | `low` \| `medium` \| `high` \| `critical`. |
| `title` | yes | string | Short description. |
| `location` | no | string | `file:line` or area. |

## Verdict meaning

- **clean** - ran, nothing to fix (e.g. async auditor on clean code).
- **pass** - reviewed, healthy with minor notes.
- **issues** - real findings to address.
- **critical** - at least one high/critical item needing prompt action.
- **generated** - produced an artifact (tests, scaffold, config) rather than a review.

## Full example

```json
{
  "id": "2026-06-16-security-atlas",
  "tool": "api-security-auditor",
  "type": "agent",
  "target": "Project Atlas",
  "date": "2026-06-16",
  "verdict": "critical",
  "summary": "Only 1/75 endpoints require authorization; EnableSensitiveDataLogging always on; AllowAnyOrigin CORS",
  "findings": [
    { "severity": "critical", "title": "Authorization coverage - verify (no FallbackPolicy)", "location": "Program.cs / endpoints" },
    { "severity": "critical", "title": "EnableSensitiveDataLogging() unconditional", "location": "ServiceCollectionExtensions.AddDatabaseSettings" },
    { "severity": "medium", "title": "CORS AllowAnyOrigin on authenticated API", "location": "ApplicationBuilderExtensions.UseCorsSettings" }
  ],
  "report": "TEST-RESULTS.md"
}
```
