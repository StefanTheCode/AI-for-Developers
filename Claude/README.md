# Claude Code Starter Skills for Developers - any stack

A curated set of **5 Claude Code skills** (plus a security agent and CLAUDE.md templates) that make Claude write production-grade code on your own projects - in **C#/.NET, TypeScript/Node, Python, Java/Kotlin, Go**, and more. Every skill detects your stack from the repo first, applies a universal checklist, then uses the idioms of your language and framework.

Curated by [Stefan Đokić - TheCodeMan](https://thecodeman.net), Microsoft MVP.

---

## ⚡ Install (one marketplace, two commands)

```shell
/plugin marketplace add StefanTheCode/dotnet-claude-starter
/plugin install claude-starter@thecodeman-claude-starter
```

That's it - the 5 skills and the security agent install into Claude Code, in every project. Skills trigger automatically when your request matches; the agent runs when you ask for what it does.

Update anytime:

```shell
/plugin marketplace update thecodeman-claude-starter
```

> Prefer a manual copy instead? See **[INSTALL.md](INSTALL.md)**.

---

## What's inside

### 🧩 Skills (5)

| Skill | What it does | Stacks |
|-------|--------------|--------|
| **orm-query-optimizer** | Finds and fixes slow ORM/database queries - N+1, over-fetching, tracking overhead, cartesian explosion, pagination, batching, missing indexes - and rewrites them. | EF Core, Prisma, TypeORM, Drizzle, SQLAlchemy, Django ORM, Hibernate/JPA, GORM |
| **async-await-auditor** | Audits async and concurrent code for deadlocks, blocking calls, lost errors, missing cancellation, leaks, and unbounded concurrency - and rewrites it. | C#, JS/TS, Python asyncio, Java/Kotlin, Go, Rust |
| **clean-architecture-scaffolder** | Scaffolds Clean Architecture or Vertical Slice structure with the dependency rule wired, a working sample feature, and an architecture guardrail test. | .NET, NestJS/Express, FastAPI, Spring Boot, Go |
| **benchmark-setup** | Sets up correct micro-benchmarks (warmup, baselines, allocations, noise) and explains the numbers. | BenchmarkDotNet, JMH, pytest-benchmark/pyperf, Vitest bench/tinybench/mitata, Go testing.B + benchstat, Rust criterion |
| **test-coverage-gap-finder** | Finds the *meaningful* test gaps - untested logic, uncovered branches, risky code - and prioritizes by risk, not vanity percentage. | coverlet, Vitest/Jest/c8, coverage.py/pytest-cov, JaCoCo, go test -cover |

### 🤖 Bonus agent (1)

| Agent | What it does |
|-------|--------------|
| **api-security-auditor** | Audits any backend (ASP.NET Core, Express/NestJS, FastAPI/Django, Spring Boot, Go net/http/Gin) against the OWASP Top 10 and OWASP API Security Top 10 - access control and IDOR/BOLA, auth/JWT, injection, secrets, CORS, mass assignment, SSRF, rate limits, vulnerable dependencies - and produces a ranked report with fixes. |

### 📄 Bonus templates

| Template | What it does |
|----------|--------------|
| **CLAUDE-md-template.md** | A stack-agnostic `CLAUDE.md` for your repo root: stack, build/test/lint/run commands, structure, conventions, a never-do list, and a definition of done - with fill-in instructions at the top. |
| **dotnet-CLAUDE-md-template.md** | The filled-in .NET 10 / C# 14 example. |
| **typescript-CLAUDE-md-template.md** | A filled-in NestJS + Prisma example. |
| **python-CLAUDE-md-template.md** | A filled-in FastAPI + SQLAlchemy example. |

---

## Skill vs. agent

**Skill** - a focused capability Claude loads automatically when your request matches it. It does the thing.

**Agent** - a specialist that explores your codebase on its own and produces a report. You invoke it by asking for what it does.

---

## How to use

Install, then just talk to Claude Code naturally:

```
> This EF query is slow, optimize it
> This Prisma query is slow - check src/orders
> Find N+1 queries in my Django views
> Review this asyncio code for blocking calls
> Find floating promises in this Node service
> Check this Go service for goroutine leaks
> Scaffold a clean architecture solution in .NET
> Set up a FastAPI project with vertical slices
> Benchmark these two functions
> What's not tested in this project?
> Audit the security of this API      <- runs the security agent
```

For the CLAUDE.md template, copy `templates/CLAUDE-md-template.md` (or the example for your stack) to your repo root as `CLAUDE.md` and fill in the blanks - instructions are at the top of the file.

---

## Want the full toolkit?

This is 5 skills. The full **[AI for Developers community on Skool](https://www.skool.com/ai-for-dotnet-developers/about)** has the whole set - **49 skills and 7 specialist agents** - plus lessons on Claude Code and agent workflows, building real AI features into your apps, and live office hours. It's where I actually teach you how to use AI in real development work, not just hand you files.

**Open on a 7-day free trial** -> [join AI for Developers](https://www.skool.com/ai-for-dotnet-developers/about)

📬 Weekly newsletter on .NET, architecture & AI (21,000+ developers): [thecodeman.net](https://thecodeman.net) · ▶️ [YouTube](https://www.youtube.com/@thecodeman_)

---

*Built by [Stefan Đokić](https://thecodeman.net) · [LinkedIn](https://www.linkedin.com/in/djokic-stefan/) · [X](https://x.com/TheCodeMan__)*
