# .NET Pack

Optional add-on for the AI for Developers Toolkit. The core toolkit (`toolkit/skills`, `toolkit/agents`) works in any stack and already covers .NET as a first-class stack. This pack holds the skills that only make sense in .NET: low-level C# performance work and maintenance of legacy .NET Framework systems.

## Install

```
/plugin install dotnet-pack@thecodeman-ai-toolkit
```

## Skills

| Skill | What it does |
|---|---|
| `span-memory-refactor` | Refactors hot C# parsing, slicing and buffer code to `Span<T>` / `ReadOnlySpan<T>` / `Memory<T>` to cut allocations, and flags where spans don't fit. |
| `legacy-package-migrator` | Moves a .NET Framework solution from `packages.config` to `PackageReference`, then to Central Package Management (`Directory.Packages.props`), removing version drift across projects. |
| `adonet-data-access-auditor` | Audits classic ADO.NET (`SqlConnection` / `SqlCommand` / `SqlDataReader` / `DataSet`) for SQL injection, leaked connections and readers, repeated reads and missing transactions, and rewrites the code. |
| `winforms-maintainer` | Fixes cross-thread errors, frozen UI, disposal and event-handler leaks, and `DataGridView` / `BindingSource` issues in existing WinForms apps with minimal, behavior-preserving changes. No UI rewrite. |

These skills pair with the core `legacy-modernization-assistant` agent, which assesses a .NET Framework codebase, picks the destination (consolidate on .NET Framework 4.8 or port to modern .NET) and hands the concrete edits to the skills above, and with `framework-upgrade-agent` for the version bumps.

## Example: legacy-pack

`examples/legacy-pack/sample-legacy-erp` is a small, deliberately broken .NET Framework solution with **19 planted defects** across a classic ADO.NET data layer, a WinForms form, and two `packages.config` files with real version drift. Point each skill (and the `legacy-modernization-assistant` agent) at it and compare the output with `examples/legacy-pack/VALIDATION-REPORT.md`. Baseline: 19/19 detected.

It's a teaching and validation fixture, not a runnable app. See `examples/legacy-pack/README.md` for ready-to-use prompts.
