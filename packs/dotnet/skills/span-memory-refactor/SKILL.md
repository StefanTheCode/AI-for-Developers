---
name: span-memory-refactor
description: (.NET) Refactor hot C# code to use Span<T>, ReadOnlySpan<T>, and Memory<T> to cut allocations in parsing, slicing, and buffer work. Use this skill whenever the user wants to use Span/Memory, avoid substring/array-copy allocations, optimize parsing or string slicing, use stackalloc, or asks "how do I use Span here / make this allocation-free / speed up this parsing". Always use this skill for Span/Memory refactors; it rewrites safely and flags where spans don't fit.
category: Performance
version: 1.0.0
---

# Span<T> / Memory<T> Refactor Advisor

Rewrite hot parsing/slicing/buffer code to use `Span<T>`/`ReadOnlySpan<T>` (and `Memory<T>` for async) so it stops allocating intermediate strings and arrays - safely, and only where it pays.

## Input - point it at your code
Works on a target: a **file**, **folder**, **project**, or **GitHub URL**. Find candidates: `grep -rn "Substring\|Split(\|ToCharArray\|new byte\[\|Buffer" --include=*.cs <target>`.

## Common refactors
**Substring → slice (no allocation):**
```csharp
// allocates a new string per call
var year = date.Substring(0, 4);
// no allocation: a view over the original
ReadOnlySpan<char> year = date.AsSpan(0, 4);
int y = int.Parse(year);
```

**Split in a hot loop → `MemoryExtensions` enumerator / manual slicing:**
```csharp
ReadOnlySpan<char> rest = line;
int comma;
while ((comma = rest.IndexOf(',')) >= 0)
{
    ReadOnlySpan<char> field = rest[..comma];
    // use field without allocating
    rest = rest[(comma + 1)..];
}
```

**Small temporary buffer → `stackalloc` (no heap):**
```csharp
Span<byte> buffer = stackalloc byte[32];   // keep small; this is stack memory
```

**Async boundaries → `Memory<T>`** (you can't store a `Span<T>` across `await`; use `Memory<T>`/`ReadOnlyMemory<T>`).

## Guardrails the skill enforces
- `Span<T>` is a `ref struct`: **can't** be a field, captured in a lambda, used across `await`, or boxed. Use `Memory<T>` there.
- Keep `stackalloc` **small and bounded** (no user-controlled size) to avoid stack overflow.
- Don't return a span over `stackalloc`/local memory - it dangles.

## Principles
- **Only refactor hot paths.** Spans add cognitive cost; cold code stays readable.
- The win is **eliminating intermediate allocations** (substrings, split arrays), not raw arithmetic.
- Measure with `[MemoryDiagnoser]` - confirm allocations actually dropped.

## How to use it & best prompts
"Make this parser allocation-free with Span", "replace these Substring calls with slices", "use stackalloc for this buffer", "why can't I use Span across await here". Pairs with `memory-allocation-analyzer` and `benchmark-setup`.
