# How to use - ADO.NET Data Access Auditor

**What it is.** A skill that audits classic ADO.NET code (`SqlConnection` / `SqlCommand` / `SqlDataReader` / `DataSet`) for the three things that go wrong in old data layers - **SQL injection**, **leaked connections/commands/readers**, and **wasteful repeated reads** - then rewrites the code with a one-line reason per fix. This is for the *old* stack, not EF Core.

**When to reach for it.** Hand-written DAL/repository code on `SqlCommand`, string-concatenated SQL, connection leaks, `using`/`Dispose` doubts, reading the same column multiple times, missing transactions, or a data layer you want hardened before a migration.

**How to use it.** Point it at your code - no pasting needed. Give it a **file**, a **`Data/` folder**, a whole **project/solution**, or a **GitHub URL** (it clones and scans). Example prompts:
- "Audit this DAL for SQL injection and connection leaks."
- "Is this ADO.NET code safe and disposing everything properly?"
- "This reads the same column several times per row - clean it up."

**How to get the best out of it.**
- **Say how the connection string is obtained** (config, DI, hardcoded) so it can wire the fix realistically.
- **Tell it which calls must be atomic** - it'll wrap them in one transaction with proper rollback.
- **Mention if it should go async** - it'll switch to `OpenAsync`/`ExecuteReaderAsync` and thread a `CancellationToken`.

**What it won't do.** It won't rip out `DataSet`/`DataTable` blindly - in WinForms that's often load-bearing for binding. Column-ordinal and isolation-level choices that depend on your real schema come marked "verify". It preserves behavior; if a fix changes results (e.g. NULL handling) it says so.
