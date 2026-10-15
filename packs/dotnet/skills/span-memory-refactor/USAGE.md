# How to use - Span<T> / Memory<T> Refactor Advisor

**What it is.** Refactors hot parsing/slicing/buffer code to `Span<T>`/`ReadOnlySpan<T>`/`Memory<T>` to remove intermediate string/array allocations - safely, with the ref-struct rules enforced.

**When to reach for it.** Hot parsing, string slicing, or buffer manipulation that allocates.

**How to use it.** Point it at the code (path or GitHub URL). Example prompts:
- "Make this parser allocation-free with Span."
- "Replace these Substring calls with slices."
- "Why can't I use Span across await here?"

**Get the best out of it.** Point it at genuinely hot code - spans add complexity, so cold code stays as-is. Verify the allocation drop with `benchmark-setup`. Pairs with `memory-allocation-analyzer`.

**Won't do.** It won't span-ify cold/readable code for marginal gains, and it enforces the `ref struct` rules (no fields, no captures, no `await`).
