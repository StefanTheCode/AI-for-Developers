# migration-assessment.json - what the scanner writes and what you edit

`scan.cs` writes the whole file. `render.cs` reads it and recomputes all totals. Edit only the fields marked **you**.

```jsonc
{
  "schemaVersion": 1,
  "generatedAt": "2026-11-25 15:22",
  "tool": "AI for Developers Toolkit - Migration Pack (beta)",
  "solution": { "name": "LegacyShop", "path": "LegacyShop.sln" },

  "summary": {                              // you
    "verdict": "go-with-conditions",        // go | go-with-conditions | not-now
    "headline": "One plain sentence.",
    "destination": "Port to .NET 10 incrementally (strangler fig) ...",
    "verdictReason": "2-4 sentences."
  },

  "projects": [{                            // scanner
    "name": "LegacyShop.Web", "path": "LegacyShop.Web/LegacyShop.Web.csproj",
    "kind": "web",                          // web | wcf | library | desktop | console
    "sdkStyle": false, "targetFramework": "v4.8", "loc": 812,
    "packagesConfig": true, "usesSystemWeb": true,
    "projectReferences": ["LegacyShop.Core", "LegacyShop.Data"],
    "fileCounts": { "aspx": 6, "ascx": 1, "master": 1, "ashx": 1, "svc": 0, "cshtml": 4, "edmx": 0, "cs": 31 },
    "packages": [{ "id": "EntityFramework", "version": "6.4.4", "status": "bridge", "note": "..." }]
  }],

  "units": [{                               // scanner, reviewed by you
    "id": "webforms-page-product-aspx",
    "kind": "webforms-page",                // webforms-page | user-control | master-page | webapi-controller | mvc-controller
                                            // | wcf-service | ef-context | http-handler | http-module | global-asax | library
    "name": "Product.aspx", "project": "LegacyShop.Web", "file": "LegacyShop.Web/Product.aspx",
    "size": "M",                            // you may change, with a note
    "category": "translation",              // mechanical | translation | verification | decision | environment
    "hoursLow": 4, "hoursHigh": 8,          // you may change, with a note
    "target": "Razor Pages",                // you: check against webforms-target-rules.md
    "targetReason": "...",                  // you
    "metrics": { "serverControls": 14, "updatePanels": 1, "...": 0 },
    "notes": ["..."],                       // you may add
    "humanTasks": ["..."]                   // you
  }],

  "crossCutting": [{                        // scanner (you may add items, e.g. a third-party control suite)
    "id": "auth-decision", "name": "Authentication & authorization design",
    "category": "decision", "hoursLow": 4, "hoursHigh": 8, "humanTasks": ["..."]
  }],

  "blockers": [{                            // scanner
    "signal": "BinaryFormatter", "severity": "hard", "count": 3,
    "files": ["LegacyShop.Web/Infrastructure/DiskCache.cs"], "meaning": "...", "fix": "..."
  }],

  "risks": [{ "title": "...", "impact": "high", "mitigation": "..." }],   // you
  "questions": ["..."],                                                  // you
  "nextSteps": ["..."],                                                  // you

  "effortModel": {                          // change only with real team data
    "hoursPerDay": 6, "reviewOverhead": 0.25, "fixedCostScale": 0.5, "totalLoc": 1398,
    "aiShare": { "mechanical": 0.9, "translation": 0.7, "verification": 0.8, "decision": 0, "environment": 0.3 },
    "assumptions": ["..."]
  },
  "totals": { }                             // scanner; render.cs recomputes it
}
```

## Adding your own items

- **A unit the scanner missed** (e.g. a Windows service, a scheduled job): add it to `units` with a unique `id`, a `kind` from the list, a `category`, hours and a target.
- **Cross-cutting work** (a third-party control suite, a reporting engine, a data migration): add it to `crossCutting`. Put decisions in `decision`, building work in `translation`, hosting work in `environment`.
