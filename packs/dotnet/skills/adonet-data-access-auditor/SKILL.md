---
name: adonet-data-access-auditor
description: (.NET) Audit and fix classic ADO.NET data-access code (SqlConnection / SqlCommand / SqlDataReader / DataSet) on .NET Framework. Use this skill whenever the user shares raw ADO.NET code, a DAL / repository built on SqlCommand, stored-proc calls, string-concatenated SQL, or mentions SQL injection, parameterized queries, connection leaks, "using / Dispose", reading the same column multiple times, transactions, or "why does my old data layer leak connections / is it safe". Always use this skill for classic ADO.NET review instead of answering from memory; it applies a fixed security + correctness + performance checklist and rewrites the code.
category: Legacy (.NET)
---

# ADO.NET Data Access Auditor

Audit classic ADO.NET code for the three things that go wrong in 20-year-old data layers - **SQL injection**, **leaked connections/commands/readers**, and **wasteful repeated reads** - then return the rewritten code with a one-line explanation per fix. This is for the *old* stack (`System.Data.SqlClient` / `Microsoft.Data.SqlClient`, `SqlDataReader`, `DataSet`/`DataTable`), not EF Core.

## When this runs

The user has hand-written ADO.NET (a DAL, a repository, a `Helper` class full of `SqlCommand`s) and wants it made safe, leak-free, and faster - or reviewed before a migration. They may paste code or point at a file. If nothing is provided, ask for the data-access method(s) and how the connection string is obtained.

## Input - point it at your code

You don't need pasted snippets. Work from whatever target the user gives:

- **A single file** - `path/to/OrderRepository.cs`
- **A folder** - a `Data/` or `DAL/` directory
- **A whole project or solution** - a `.csproj` / `.sln` or the repo root
- **A GitHub URL** - clone first: `git clone --depth 1 <url> /tmp/repo && cd /tmp/repo`

Find the ADO.NET yourself with Glob/Grep/Bash instead of asking for a paste:
```
grep -rl "SqlCommand\|SqlConnection\|SqlDataReader\|SqlDataAdapter" --include=*.cs <target> | grep -vi "/obj/\|/bin/"
grep -rn "ExecuteReader\|ExecuteNonQuery\|\" + \|GetOrdinal" --include=*.cs <target>
```
On a large data layer, prioritize the worst offenders (string-built SQL, undisposed connections) and tell the user which files you scanned.

## Workflow

1. **Read the code and the data flow.** Identify every `SqlConnection`, `SqlCommand`, `SqlDataReader`, `DataAdapter`, `DataSet`, and where user input enters the SQL.
2. **Run the checklist below** against every command.
3. **Rewrite** the problematic methods. Preserve behavior unless you find a bug (call bugs out separately).
4. **Explain each change** in one line - what was wrong, why it matters, what the fix does.
5. **Flag what you cannot verify** (e.g. column ordinals depend on the actual proc; mark "verify").

## The checklist

Apply every item. For each finding: problem -> impact -> fix.

### 1. SQL injection / string-built SQL (security, do this first)
The classic sin: `"SELECT * FROM Users WHERE Name = '" + name + "'"`.
- **Detect:** `+`, `string.Format`, `$"..."`, or `StringBuilder` assembling SQL that contains any variable that could carry user input.
- **Fix:** parameterize. `cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = name;` (typed + sized - avoids implicit conversions and plan-cache bloat). Never use `AddWithValue` for `varchar`/`char` columns or for anything performance-sensitive; it infers the wrong type and causes implicit conversions that kill index usage. Table/column names can't be parameterized - whitelist them against a fixed allow-list instead of concatenating.

### 2. Undisposed connections / commands / readers (leaks)
Old code opens a `SqlConnection` and never guarantees close on exception, or returns a `SqlDataReader` the caller forgets to dispose.
- **Detect:** `new SqlConnection(...)` without `using`; `conn.Open()` with no `try/finally` or `using`; a reader/command created without `using`; connections stored in fields and reused.
- **Fix:** wrap `SqlConnection`, `SqlCommand`, and `SqlDataReader` each in `using` (or `using var`). Connections are cheap because of connection **pooling** - open late, close early, one per unit of work. Do **not** cache/share a single `SqlConnection` across calls or threads. If a method returns a reader, either materialize inside the method or accept that ownership transfer is a code smell - prefer returning a `List<T>`/DTO.

### 3. Reading the same value multiple times (a classic legacy complaint)
`reader["Total"]` called several times, or `reader.GetOrdinal("X")` inside the row loop.
- **Detect:** the same `reader["col"]` / indexer accessed more than once per row; `GetOrdinal` called inside the `while (reader.Read())` loop; boxing via `reader["col"]` then casting.
- **Fix:** resolve ordinals **once, before** the loop (`int totalOrd = reader.GetOrdinal("Total");`), then use typed getters inside the loop (`reader.GetDecimal(totalOrd)`), reading each column exactly once into a local. Typed getters avoid boxing; caching ordinals avoids a hash lookup per column per row.

### 4. NULL handling
- **Detect:** `(string)reader["Name"]` / direct casts that throw on `DBNull`; comparing to `null` instead of `DBNull.Value`.
- **Fix:** `reader.IsDBNull(ord) ? null : reader.GetString(ord)`. For parameters, send `(object?)value ?? DBNull.Value`.

### 5. Transactions & multi-statement units of work
- **Detect:** several `ExecuteNonQuery` calls that must succeed together but each runs on its own; a `SqlTransaction` opened but not assigned to `cmd.Transaction`; missing rollback on exception.
- **Fix:** one `using var tx = conn.BeginTransaction();`, assign `cmd.Transaction = tx;` on every command, `tx.Commit()` at the end inside the try, and rely on `using` to roll back if `Commit` never runs (dispose of an uncommitted transaction rolls back). Set an appropriate isolation level explicitly if the default matters.

### 6. Async
- **Detect:** `ExecuteReader()`, `ExecuteNonQuery()`, `Open()`, `reader.Read()` in code that is (or should be) async, blocking a thread on I/O.
- **Fix:** `await conn.OpenAsync(ct)`, `await cmd.ExecuteReaderAsync(ct)`, `await reader.ReadAsync(ct)`, `ExecuteNonQueryAsync(ct)` - and thread a `CancellationToken` through. (Note: `System.Data.SqlClient` is legacy; `Microsoft.Data.SqlClient` has better async + is where fixes land - suggest it, but it's an optional swap.)

### 7. Command setup hygiene
- **Detect:** `CommandType.Text` for what is really a stored proc; `CommandTimeout` never set for known-long queries; re-creating the same `SqlCommand` in a loop instead of reusing with changed parameter values; `SELECT *` feeding a reader that uses two columns.
- **Fix:** set `CommandType.StoredProcedure` and pass the proc name; set a deliberate `CommandTimeout` for long ops; for batch inserts prepare once and change parameter values per iteration (or use `SqlBulkCopy` for large loads); select only needed columns.

### 8. DataSet/DataAdapter overuse
- **Detect:** `SqlDataAdapter.Fill(dataSet)` to then read a handful of scalar values or loop once.
- **Fix:** a `SqlDataReader` is lighter for forward-only reads; keep `DataSet`/`DataTable` only where disconnected editing/binding genuinely needs it (common in WinForms grids - don't rip it out blindly).

## Reference rewrite (the shape to aim for)

```csharp
public async Task<List<OrderDto>> GetOrdersAsync(int customerId, CancellationToken ct)
{
    const string sql = @"SELECT Id, Total, CreatedUtc
                         FROM dbo.Orders
                         WHERE CustomerId = @CustomerId";

    using var conn = new SqlConnection(_connectionString);
    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;   // typed + parameterized

    await conn.OpenAsync(ct);
    using var reader = await cmd.ExecuteReaderAsync(ct);

    // ordinals resolved ONCE, before the loop
    int idOrd    = reader.GetOrdinal("Id");
    int totalOrd = reader.GetOrdinal("Total");
    int dateOrd  = reader.GetOrdinal("CreatedUtc");

    var result = new List<OrderDto>();
    while (await reader.ReadAsync(ct))
    {
        result.Add(new OrderDto
        {
            Id      = reader.GetInt32(idOrd),                                  // typed getter, read once
            Total   = reader.GetDecimal(totalOrd),
            Created = reader.IsDBNull(dateOrd) ? null : reader.GetDateTime(dateOrd)
        });
    }
    return result;
}
```

## Output format

```
## Summary
<one paragraph: the security + leak + perf headline findings>

## Findings
1. [SQL injection] <file/line> - <problem> -> <fix>
2. [Leak / no using] ... -> ...
3. [Repeated read] ... -> ...

## Rewritten code
<the fixed method(s), ready to paste>

## Verify on your side
<column ordinals vs real schema, isolation level choice, System.Data vs Microsoft.Data.SqlClient>
```

## Principles
- **Security first, always.** Any string-built SQL touching input is the top finding, above style or perf.
- **Every disposable in a `using`.** Connection, command, reader, transaction - no exceptions, pooling makes it cheap.
- **Read each column once, resolve ordinals once.** Typed getters over indexers.
- **Don't rip out DataSet blindly** - in WinForms it's often load-bearing for binding.
- **Preserve behavior; call out bugs separately.** If a fix changes results (e.g. NULL handling), say so.
