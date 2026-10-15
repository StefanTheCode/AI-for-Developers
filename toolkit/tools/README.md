# Tools

## CLAUDE.md Generator for .NET

`claude-md-generator.html` - an interactive, single-file tool that generates a tailored **CLAUDE.md** for your .NET project.

Claude Code starts every session knowing nothing about your repo, so it defaults to generic C# - controllers, repositories, AutoMapper. A `CLAUDE.md` at your repo root fixes that: Claude reads it automatically and writes *your* .NET from the first prompt.

### Use it

- **Locally:** open `claude-md-generator.html` in any browser - no build, no signup.
- Pick your stack, architecture, conventions, and the patterns Claude should **never** suggest.
- Copy or download the generated `CLAUDE.md` and drop it in your repo root.

### What it covers

Stack & .NET version · web style · architecture (VSA / Clean / DDD / Modular Monolith) · data/ORM · validation · error handling (Result pattern) · auth · API docs & versioning · object mapping · ID strategy · serialization · Extras (HybridCache, Polly v8, Serilog, OpenTelemetry, Options, Aspire, health checks, rate limiting, feature flags, Central Package Management, Docker, CI) · and a **Never suggest** section that blocks bad defaults before Claude walks down them.

Prefer a ready-made starting point? See [`../templates/dotnet-CLAUDE-md-template.md`](../templates/dotnet-CLAUDE-md-template.md).

---

Made by [Stefan Đokić - TheCodeMan](https://thecodeman.net). Free to use & share.
