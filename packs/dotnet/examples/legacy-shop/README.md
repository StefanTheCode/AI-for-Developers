# LegacyShop - a real .NET Framework 4.8 app to migrate

A small but complete web shop, written the way many 2013-era .NET apps were: WebForms pages, a few Web API 2 endpoints and an MVC 5 report in the same project, a WCF service, EF6 and a shared class library that reaches into `HttpContext.Current`.

It **builds and runs** (unlike the Legacy Pack's `sample-legacy-erp`, which is a static fixture). That's the point: the Migration Pack moves it to .NET 10 piece by piece, and parity tests prove the new app answers like the old one.

`TRAPS.md` is the answer key: 29 migration traps and 9 behavior quirks.

## What's inside

```text
LegacyShop.sln
├── LegacyShop.Web        WebForms (6 pages, master, user control, .ashx), Web API 2, MVC 5 report   http://localhost:5100
├── LegacyShop.Inventory  WCF service (basicHttpBinding, FaultContract)                              http://localhost:5101/InventoryService.svc
├── LegacyShop.Data       EF6 code-first, LocalDB, seed data
└── LegacyShop.Core       shared library: pricing rules, HttpContext.Current, ConfigurationManager
```

Demo users: `admin@legacyshop.local` / `admin123` and `ana@legacyshop.local` / `ana123`. Coupon: `LEGACY10`.

## Run it (Windows)

You need Visual Studio 2022 or 2026 with the **ASP.NET and web development** workload. It brings MSBuild, the web build targets, IIS Express and SQL Server Express LocalDB.

**One command - build, run, smoke test:**

```powershell
cd packs\dotnet\examples\legacy-shop
.\verify.ps1
```

It restores the NuGet packages, builds the solution, starts both sites in IIS Express, checks 17 things (pages, API, MVC report, image handler, WCF, auth redirects, the HTTP module header) and prints a PASS/FAIL table. Add `-KeepRunning` to leave the sites up and click around.

**Or in Visual Studio:** open `LegacyShop.sln`, set **multiple startup projects** (LegacyShop.Web and LegacyShop.Inventory), press F5.

The first request creates and seeds the `LegacyShop` database in LocalDB. Start the **web app first** - its `Application_Start` sets the EF initializer that seeds the data.

## Use it with the Migration Pack

1. **Assess:** "Assess LegacyShop.sln for migration to .NET 10" - you get `migration/migration-assessment.html`. Compare it with `TRAPS.md`.
   Want to see the result first? `migration/` already holds the report generated for this app - open `migration/migration-assessment.html` in a browser. Running the assessment again overwrites it.
2. **Migrate:** run the `dotnet-migration-orchestrator` agent on the solution. It builds a safety net of parity tests first, puts a .NET 10 host with YARP in front, and moves the app piece by piece.

Work on a copy or a branch - the agents change files.
