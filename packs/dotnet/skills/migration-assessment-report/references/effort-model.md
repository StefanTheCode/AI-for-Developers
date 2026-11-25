# How the estimate is calculated

Short version: every unit of work gets a size and an hour range from what the scanner counts. Each unit belongs to one kind of work. Each kind of work has an "AI share" - how much of it an agent can take over. "With AI" means the agent does its share and a developer reviews it.

## Units and base hours (without AI)

| Unit | S | M | L | How size is decided |
|---|---|---|---|---|
| WebForms page | 2-4 h | 4-8 h | 10-18 h | complexity points (below) |
| User control / master page | half of a page | | | same points |
| Web API 2 controller | 1-3 h | 3-6 h | 8-14 h | actions: <=3 / <=8 / more |
| MVC 5 controller | 2-4 h | 4-8 h | 10-16 h | actions (+1-2 h per child action) |
| WCF service | 2 + 0.5 h per operation | ... | 4 + 1.5 h per operation | operations |
| EF context | 2 + 0.5 h per DbSet + 0.25 h per EF6-only API use | ... | 4 + 1.25 h per DbSet + 0.75 h per use | |
| Class library | 2-4 h | 6-12 h | 16-40 h | LOC: <500 / <2000 / more |
| HTTP module / handler | 1-3 h | | | |
| Global.asax | 2-4 h | 3-6 h (5+ events) | | |

**WebForms complexity points** = server controls / 5 + UpdatePanels x 3 + data controls x 2 + validators x 0.5 + postback handlers + ViewState uses + session uses x 0.5 + data access in code-behind x 2 + inline-edit grid x 4 + user controls + code-behind lines / 40. Under 8 is S, under 20 is M, above is L.

## Cross-cutting work

Added when the scanner finds the signal: auth design + build, session strategy + build, configuration, machineKey -> Data Protection, BinaryFormatter replacement, System.Drawing, WCF clients, bundling, project/package conversion. Always added: the safety net (parity + characterization tests), the new .NET 10 host with YARP, rollout, and shutting down the old app.

Fixed costs (host, auth, session, config, rollout, shutdown) are scaled by solution size: x0.5 under 3,000 lines of C#, x1 up to 30,000, x1.5 above.

## AI share per kind of work

| Kind | AI share | Examples |
|---|---|---|
| Mechanical | 90% | project files, packages, namespaces, config, Program.cs |
| Translation | 70% | pages, controllers, endpoints, EF mappings, CoreWCF |
| Verification | 80% | parity, characterization and contract tests |
| Decision | 0% | auth model, session, hosting, UI targets, data |
| Environment | 30% | hosting, CI/CD, rollout, monitoring |

**With AI** = hours x (1 - AI share) + hours x AI share x 25% review overhead.

Example: a 10-hour translation unit -> 10 x 0.3 + 10 x 0.7 x 0.25 = 4.75 hours with AI.

## Days and size

- Days = hours / 6 focused hours a day, one developer.
- Size from the midpoint of the "without AI" range: S under 80 h, M under 240 h, L under 800 h, XL above.

## Calibrate it

After the first 3 migrated slices, compare real hours with the estimate and adjust `effortModel` in the JSON (`hoursPerDay`, `aiShare`, `reviewOverhead`), then re-render. Say in the report that it was calibrated and on what.
