# How to use - Result Pattern Scaffolder

**What it is.** Adds errors-as-values to your codebase so expected failures (validation, not found, conflict) are returned instead of thrown, plus a single place that maps those errors to HTTP responses (Problem Details). It uses the idiom of your stack: `Result<T>` (or ErrorOr / FluentResults / OneOf) in C#, discriminated unions or neverthrow in TypeScript, Ok/Err dataclasses in Python, sealed interfaces / Either in Java and Kotlin, and typed/sentinel errors with `errors.Is`/`errors.As` in Go.

**When to reach for it.** When validation/not-found/conflict cases are handled with exceptions, error-to-status mapping is scattered across handlers, or you want railway-style error handling.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Add a Result pattern and convert these handlers off exceptions." (.NET)
- "Introduce Result<T> but match the OneOf I already use."
- "Result type for my Express services with one error-to-HTTP mapper."
- "Errors as values in my FastAPI app - or tell me if exception handlers fit better."
- "Sealed Result type for our Java 21 Spring services."
- "Clean up error handling in my Go handlers with typed errors and one writeError."

**Get the best out of it.** Let it detect an existing Result/Either type or error convention and extend it rather than adding a competing one. Combine with `cqrs-setup` so handlers return results end to end.

**What you get.** The detected convention and the choice made, the Result/Error type (or error set), the single HTTP mapping, a before/after table of converted throw sites, and a list of what intentionally stays an exception.

**Won't do.** It won't turn real infrastructure exceptions into Results - only expected, domain-level outcomes. It won't add a generic Result type to Go, where `(T, error)` already is the pattern.
