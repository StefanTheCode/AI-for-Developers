# migration-safety-net - usage

```text
Build the parity safety net for examples/legacy-shop before we migrate anything.
```

```text
Add characterization tests for PriceCalculator - lock the current behavior, quirks included.
```

```text
Record the parity baseline against http://localhost:5100, then verify the YARP host on http://localhost:5200.
```

You get `tests/<Solution>.ParityTests` (HTTP + Playwright flows, record/verify), `tests/<Solution>.CharacterizationTests` (golden master), and `migration/accepted-differences.md`.

Rule of the pack: a route moves to .NET 10 only when its parity tests are green.
