# EF Core - Advanced Patterns Reference

Read this when the basic checklist isn't enough - high-throughput paths, repeated hot queries, or tricky split-query / raw-SQL situations.

## Compiled queries

For hot queries executed thousands of times, compile once to skip query-plan caching overhead.

```csharp
private static readonly Func<AppDbContext, int, Task<Customer?>> _getById =
    EF.CompileAsyncQuery((AppDbContext db, int id) =>
        db.Customers.AsNoTracking().FirstOrDefault(c => c.Id == id));

public Task<Customer?> GetByIdAsync(int id) => _getById(_db, id);
```

Use only on proven hot paths; it adds boilerplate.

## DbContext pooling

For high-traffic APIs, pool contexts to cut allocation cost:

```csharp
builder.Services.AddDbContextPool<AppDbContext>(o =>
    o.UseSqlServer(conn));
```

Caveat: don't store request state in the context; pooling reuses instances.

## Split query trade-offs

| | Single query (default) | Split query |
|---|---|---|
| Round-trips | 1 | N (one per collection) |
| Row duplication | Yes (cartesian) | No |
| Data consistency | Snapshot-consistent | Risk of inconsistency between queries |
| Best for | Small/related includes | Multiple large collections |

Enable per query: `.AsSplitQuery()`. Globally: `o.UseSqlServer(conn, x => x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))`.

## Bulk update/delete (EF Core 7+)

```csharp
// One UPDATE statement, no entities loaded:
await db.Orders
    .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < cutoff)
    .ExecuteUpdateAsync(s => s
        .SetProperty(o => o.Status, OrderStatus.Expired)
        .SetProperty(o => o.UpdatedAt, now));

// One DELETE statement:
await db.AuditLogs.Where(a => a.CreatedAt < retentionCutoff)
    .ExecuteDeleteAsync();
```

Note: these bypass the change tracker and SaveChanges interceptors - won't fire domain events or audit logic that lives there.

## Raw SQL escape hatch

When LINQ can't express it or you need a tuned query:

```csharp
var results = await db.Database
    .SqlQuery<OrderSummary>($"SELECT ... WHERE CustomerId = {customerId}")
    .ToListAsync();
```

`FromSql`/`SqlQuery` with interpolation is parameterized (safe). Never string-concatenate user input.

## Inspecting generated SQL

```csharp
// At query build time, no execution:
var sql = query.ToQueryString();

// Log everything during dev:
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information)
              .EnableSensitiveDataLogging(); // dev only
```

## Tracking nuances

- `AsNoTrackingWithIdentityResolution()` - no tracking, but de-duplicates the same entity appearing multiple times in a graph (prevents duplicate object instances).
- `QueryTrackingBehavior.NoTracking` as a global default for read-heavy contexts; opt back in per-query with `.AsTracking()` for writes.

## Common cartesian-explosion example

```csharp
// BAD: rows = orders × items × payments
db.Orders.Include(o => o.Items).Include(o => o.Payments).ToListAsync();

// GOOD: split, or project
db.Orders
  .Select(o => new OrderDto {
      Id = o.Id,
      Items = o.Items.Select(i => new ItemDto { i.Id, i.Name }).ToList(),
      Payments = o.Payments.Select(p => new PayDto { p.Id, p.Amount }).ToList()
  }).ToListAsync();
```
