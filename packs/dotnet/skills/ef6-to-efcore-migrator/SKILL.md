---
name: ef6-to-efcore-migrator
description: (.NET) Move an Entity Framework 6 data layer to .NET 10 in two safe steps - first EF 6.5 on .NET 10 as a bridge (same model, same queries, no rewrite), then EF Core one DbContext at a time behind query parity tests. Covers DbModelBuilder fluent config, EDMX models, lazy loading, database initializers and Seed, DbFunctions/SqlFunctions, string Include, cascade-delete and orphan differences, and a baseline migration for an existing database. Use this skill whenever the user mentions EF6, EntityFramework 6, "EF6 to EF Core", EDMX, DbModelBuilder, "EF6 on .NET 8/10", lazy loading differences, or is moving a data layer in a .NET Framework migration. Always use this skill for EF6 migrations instead of answering from memory.
category: Legacy (.NET)
---

# EF6 -> EF Core Migrator (beta)

Two steps, because they solve different problems:

1. **Bridge: EF 6.5 on .NET 10.** Gets the data layer onto .NET 10 without touching the model or a single query. The web migration isn't blocked by the data layer anymore.
2. **EF Core, one context at a time.** Rewrites the mapping and the queries, proven by query parity tests against the same database.

Skip step 1 only for small models where the EF Core rewrite is quicker than the bridge (a few entities, no EDMX).

## Input

Every `DbContext`, entity, `EntityTypeConfiguration<T>`, `OnModelCreating`, `.edmx`, initializer / `Seed`, every repository or query (`Include`, `DbFunctions`, `SqlFunctions`, raw SQL), and the connection strings in config.

## Step 1 - EF 6.5 on .NET 10

1. Project to SDK-style, multi-target `net48;net10.0` (`legacy-package-migrator` does the project file).
2. Packages: `EntityFramework` 6.5.x, and for SQL Server the new provider `Microsoft.EntityFramework.SqlServer` (uses `Microsoft.Data.SqlClient`).
3. Code-based configuration - .NET 10 has no `<entityFramework>` config section:

   ```csharp
   [DbConfigurationType(typeof(System.Data.Entity.SqlServer.MicrosoftSqlDbConfiguration))]
   public class ShopContext : DbContext
   {
       public ShopContext(string connectionString) : base(connectionString) { }
       // keep a parameterless ctor for the net48 side while it still reads Web.config:
       public ShopContext() : base("name=ShopContext") { }
   }
   ```

   The .NET 10 host passes the connection string from `appsettings.json`. Watch the `Microsoft.Data.SqlClient` default `Encrypt=true` - add `TrustServerCertificate=True` only for local dev.
4. EDMX: the EF6 designer doesn't work on .NET or SDK-style projects. If the model is EDMX-based, go straight to step 2 for that context (scaffold).
5. Run the app's tests and the parity tests on both targets.

## Step 2 - EF Core, per DbContext

### 2a. Model

- EDMX or database-first: `dotnet ef dbcontext scaffold "<conn>" Microsoft.EntityFrameworkCore.SqlServer --context <Name> --output-dir Model`, then compare with the old entities property by property.
- Code-first: translate `OnModelCreating` with `references/ef6-to-efcore-mapping.md`. Keep table and column names identical (EF6's `PluralizingTableNameConvention` removal, `[Table]`, key names).
- Decide each relationship's delete behavior explicitly (`OnDelete(DeleteBehavior.Restrict / Cascade / ClientSetNull)`). EF6 conventions cascaded every required FK - that is often how order history disappears when a product is deleted. A person decides; log it.

### 2b. The existing database

Don't let EF Core create or change the schema on day one:

1. `dotnet ef migrations add Baseline` - then empty its `Up()` / `Down()` (the tables already exist).
2. `dotnet ef database update` on a copy, check the schema didn't change (`dotnet ef migrations script` shows nothing but the history table).
3. Seed data from an EF6 initializer -> `HasData` (static reference data) or a seeding step in `Program.cs` (`UseSeeding` / `UseAsyncSeeding`).

### 2c. Queries

- **Lazy loading:** EF Core doesn't lazy load by default. For each place that touches a navigation after the query, add `Include` / a projection. `UseLazyLoadingProxies()` (Microsoft.EntityFrameworkCore.Proxies) is the quick bridge - note every use as debt.
- **Contexts kept open** for later lazy loads (repositories returning entities from an undisposed context): fix with projections to DTOs inside the `using`.
- `DbFunctions.TruncateTime(x)` -> `x.Date`; `SqlFunctions.*` -> `EF.Functions.*`.
- `Include("Lines")` -> `Include(o => o.Lines)`.
- Client-side evaluation that EF6 allowed silently throws in EF Core - move it after `AsEnumerable()` on purpose, or rewrite it to translate.

### 2d. Query parity tests

For every repository method / query: run the EF6 version and the EF Core version against the same seeded test database (LocalDB copy or Testcontainers SQL Server) and compare the results (counts, ordering, values, nulls). Template: `references/QueryParityTests.cs`. Watch especially: ordering without `OrderBy`, decimal precision, `DateTime` kind, null navigations, grouping results.

### 2e. Switch

Swap the registrations in the new host to the EF Core context (`AddDbContext<ShopContext>(o => o.UseSqlServer(...))`), run the full parity suite, commit: `migrate: ShopContext -> EF Core`. The EF6 context is deleted at decommission.

## Rules

- Same database, same schema, no migrations that change tables during the move.
- Every behavior difference (cascade, orphans, lazy loading) is a person's decision, logged in `migration/accepted-differences.md`.
- One context per commit.
