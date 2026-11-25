# migration-assessment-report - usage

## Try it

```text
Assess examples/legacy-shop/LegacyShop.sln for a move to .NET 10 and give me the HTML report.
```

```text
How hard would it be to move our WebForms app to .NET 10? I need something I can show my manager.
```

```text
Re-render the migration report - I changed the targets for the admin pages in migration-assessment.json.
```

## What you get

- `migration/migration-assessment.json` - the inventory and the estimate (edit it, re-render any time)
- `migration/migration-assessment.html` - one self-contained file: diagrams, blockers, a target per WebForms page, days with and without AI, who does what, risks, questions, next steps. Works offline, prints to PDF.

## Needs

The .NET 10 SDK (`dotnet --version` -> 10.x). Nothing else - the scanner and the renderer are single-file .NET apps.

## Then

Hand the report to the `dotnet-migration-orchestrator` agent - it uses the same JSON to plan and run the migration slice by slice.
