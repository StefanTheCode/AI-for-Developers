# Legacy Pack - sample & validation

A tiny, deliberately-broken .NET Framework solution (`sample-legacy-erp`) used to demonstrate and test the legacy skills. It contains **19 planted defects** across a classic ADO.NET data layer, a WinForms form, and two `packages.config` files with real version drift.

## Try it yourself
Point each skill at this folder and watch it find the planted issues:

```
# packaging / version drift  -> legacy-package-migrator
"Migrate packs/dotnet/examples/legacy-pack/sample-legacy-erp to Central Package Management and unify versions."

# data layer                 -> adonet-data-access-auditor
"Audit packs/dotnet/examples/legacy-pack/sample-legacy-erp/LegacyErp.Data/OrderRepository.cs for SQL injection and leaks."

# UI                         -> winforms-maintainer
"Fix the cross-thread and leak issues in packs/dotnet/examples/legacy-pack/sample-legacy-erp/LegacyErp.UI/MainForm.cs."

# whole-solution triage      -> legacy-modernization-assistant (agent)
"Assess packs/dotnet/examples/legacy-pack/sample-legacy-erp and give me a staged migration plan."
```

## What "good" looks like
See **VALIDATION-REPORT.md** - every planted defect, where it is, and the fix each skill produces. Baseline: **19/19 detected**.

> This is a teaching/validation fixture, not a runnable app. It won't build - the defects are the point.
