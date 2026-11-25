# EF6 -> EF Core mapping cheat sheet

| EF6 | EF Core |
|---|---|
| `System.Data.Entity` | `Microsoft.EntityFrameworkCore` |
| `DbModelBuilder` | `ModelBuilder` |
| `EntityTypeConfiguration<T>` + `modelBuilder.Configurations.Add(...)` | `IEntityTypeConfiguration<T>` + `modelBuilder.ApplyConfigurationsFromAssembly(...)` |
| `HasRequired(x => x.A).WithMany(a => a.Bs).HasForeignKey(x => x.AId)` | `HasOne(x => x.A).WithMany(a => a.Bs).HasForeignKey(x => x.AId).IsRequired()` |
| `HasOptional(...)` | `HasOne(...)` + nullable FK |
| `WithRequiredPrincipal` / `WithRequiredDependent` (1:1) | `HasOne(...).WithOne(...).HasForeignKey<TDependent>(...)` |
| `WillCascadeOnDelete(true/false)` | `OnDelete(DeleteBehavior.Cascade / Restrict)` |
| `HasPrecision(18, 2)` | `HasPrecision(18, 2)` |
| `Property(x => x.Name).HasMaxLength(120).IsRequired()` | same |
| `modelBuilder.Conventions.Remove<PluralizingTableNameConvention>()` | EF Core uses the DbSet name - set `ToTable("Product")` or `[Table]` to keep the old names |
| `Map(m => m.ToTable(...))` (entity splitting) | `SplitToTable` |
| TPH / TPT | supported; TPC supported since EF Core 7 |
| `Database.SetInitializer(...)`, `Seed` | migrations + `HasData` / `UseSeeding` |
| `context.Database.SqlQuery<T>(sql)` | `context.Database.SqlQuery<T>($"...")` (EF Core 8+) |
| `DbFunctions.TruncateTime(d)` | `d.Date` |
| `SqlFunctions.DateDiffDay(a, b)` | `EF.Functions.DateDiffDay(a, b)` |
| `Include("Lines.Product")` | `Include(o => o.Lines).ThenInclude(l => l.Product)` |
| Lazy loading on by default | off by default; `UseLazyLoadingProxies()` or explicit `Include` |
| `Configuration.ProxyCreationEnabled` | proxies only with the Proxies package |
| `context.Entry(x).State = EntityState.Modified` | same (beware of graph behavior) |
| Orphans kept when removed from a collection | orphans deleted for required relationships - check every `Collection.Remove(...)` |
| `ObjectContext` / `IObjectContextAdapter` | not available - rewrite |
| `.edmx` | `dotnet ef dbcontext scaffold` |
