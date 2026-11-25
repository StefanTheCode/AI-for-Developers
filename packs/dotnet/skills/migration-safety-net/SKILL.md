---
name: migration-safety-net
description: (.NET) Lock the current behavior of a .NET Framework app BEFORE migrating it - HTTP parity tests (record the old app's responses, then check the new .NET 10 app or the YARP host answers the same), Playwright flow tests for postback screens, and characterization (golden master) tests for business logic. Use this skill whenever the user is about to migrate or refactor legacy code and asks for "parity tests", "characterization tests", "golden master", "how do I know the new app behaves the same", "lock the behavior first", "regression tests before migration", or before any route moves from the old app to the new one. Always use this skill to build the safety net instead of writing ad-hoc tests; it produces a reusable record/verify harness and an accepted-differences log.
category: Legacy (.NET)
---

# Migration Safety Net (beta)

Before a single line moves to .NET 10, prove what the old app does today. Then every migrated slice is checked against that proof. No green parity test, no route switch.

Three layers, cheapest first:

| Layer | Catches | Tool |
|---|---|---|
| **HTTP parity** | status codes, redirects, content type, the data on GET pages and API responses | xUnit + HttpClient + AngleSharp, record/verify |
| **Flow parity** | postback flows: add to cart, checkout, save a form, edit a grid row | xUnit + Microsoft.Playwright |
| **Characterization** | business rules in plain C# (pricing, discounts, rounding) | xUnit golden master |

## Input

The solution, and `migration/migration-assessment.json` if it exists (it lists every page, endpoint and the quirks worth locking). Read `Web.config` and the route setup to build the URL list.

## Process

### 1. Build the scenario list

Create `tests/<Solution>.ParityTests/scenarios.json`. One entry per URL a user or client hits, including the unhappy paths:

```json
[
  { "id": "home", "method": "GET", "path": "/", "compare": "html", "select": "main" },
  { "id": "product-1", "method": "GET", "path": "/Product.aspx?id=1", "compare": "html", "select": "main" },
  { "id": "product-missing", "method": "GET", "path": "/Product.aspx?id=99999", "compare": "status" },
  { "id": "api-products", "method": "GET", "path": "/api/products", "accept": "application/json", "compare": "json" },
  { "id": "api-orders-anon", "method": "GET", "path": "/api/orders/mine", "compare": "status+location" },
  { "id": "admin-anon", "method": "GET", "path": "/Admin/Products.aspx", "compare": "status+location" }
]
```

- `compare`: `status`, `status+location`, `html` (normalized text of the `select` element), `json` (normalized JSON), `bytes-type` (content type + non-empty body, for images and files).
- Cover every page, every API action (GET at least), every handler, every redirect rule (auth, admin areas), and every 404 the app produces.
- Read `references/normalization.md` and add page-specific ignore rules (`ignore` selectors) for timestamps, random ids, ads.

### 2. Generate the harness

Create an xUnit project targeting `net10.0` with `references/ParityTests.cs` and `references/Normalizer.cs` as the starting point (adapt namespaces, keep the logic). Packages: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `AngleSharp`.

Modes, set with environment variables:

| Variable | Meaning |
|---|---|
| `PARITY_MODE=record` | call `PARITY_BASE_URL`, normalize, write `baseline/<id>.txt` |
| `PARITY_MODE=verify` (default) | call `PARITY_BASE_URL`, normalize, compare with `baseline/<id>.txt` |
| `PARITY_BASE_URL` | the old app when recording, the YARP host / new app when verifying |

Recording once against the old app means the old app doesn't have to run forever - the baseline is in git.

### 3. Flow parity for postback screens

For each flow that changes state (add to cart, update quantities, apply a coupon, checkout, admin edit), write one Playwright test that drives the UI by visible labels and roles, not by WebForms ids (`ctl00$MainContent$...` won't exist in the new app). Assert what the user sees: cart count, line totals, the total, the confirmation message. Template: `references/FlowTests.cs`. Packages: `Microsoft.Playwright`, then `pwsh bin/Debug/net10.0/playwright.ps1 install chromium` once.

Use the same test against both apps by switching `PARITY_BASE_URL`. If the markup changes, put stable `data-testid` attributes into the NEW pages and keep a small selector map per app in the test.

### 4. Characterization tests for business logic

For pure logic (a `PriceCalculator`, a tax rule, an import parser): generate many inputs, run the CURRENT code, save the outputs as the golden master, and assert the same outputs forever. Include the weird cases on purpose and mark them `// QUIRK: <what>` - they are the behaviors a rewrite "fixes" by accident. Template: `references/CharacterizationTests.cs`. While the library is still `net48`, the test project targets `net48` (runs on Windows); once the library multi-targets, add `net10.0`.

Prove the tests bite: change one line of the logic on purpose, run, see red, undo.

### 5. The accepted-differences log

Create `migration/accepted-differences.md`. Every time old and new legitimately differ (401 instead of 302, JSON instead of XML, a fixed rounding bug), the test gets an entry with: scenario id, old behavior, new behavior, why, who approved it, date. A test may only be relaxed when a person approved the entry - never by the agent alone.

### 6. Run and report

- Record against the old app, commit `baseline/`.
- Verify against the old app too (should be 100% green - if not, the normalization is wrong or the page is not deterministic; fix that first).
- Report: number of scenarios, flows, characterization cases; anything non-deterministic you had to ignore and why.

## Rules

- Tests first, migration second. Never generate tests from the NEW code.
- Never edit a baseline to make a test pass. Re-record only on purpose, with a log entry.
- Keep public URLs identical in the new app during the move; parity is then a straight comparison.
- Use a disposable database (LocalDB copy, Testcontainers SQL Server) seeded the same way for both apps, so data differences don't look like code differences.
