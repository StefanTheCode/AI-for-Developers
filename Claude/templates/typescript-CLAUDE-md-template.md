<!--
HOW TO USE THIS FILE
--------------------
TypeScript / Node.js example of the generic CLAUDE-md-template.md, filled in for a NestJS + Prisma API.
1. Copy everything BELOW the line into `CLAUDE.md` at your repo root.
2. Replace the [PLACEHOLDERS] and swap any tool you don't use (Express instead of NestJS, Drizzle instead of Prisma, npm instead of pnpm).
3. Delete rules that don't apply.
Made by Stefan Đokić · TheCodeMan · free to use and adapt.
==================================================================
-->

# Project: [PROJECT NAME]

[One or two sentences: what this service does.]

Instructions for Claude Code working in this repository. Follow these by default; ask before deviating.

## Stack

- **TypeScript** (strict mode) on **Node.js [22 LTS]**
- **Framework:** NestJS
- **Data:** Prisma with PostgreSQL
- **Validation:** class-validator DTOs + global `ValidationPipe({ whitelist: true, forbidNonWhitelisted: true })`
- **Testing:** Vitest (unit) + Supertest + Testcontainers (integration)
- **Package manager:** pnpm

## Commands

- Install: `pnpm install`
- Build: `pnpm build`
- Test (all): `pnpm test`
- Test (single): `pnpm vitest run path/to/file.test.ts`
- Lint: `pnpm lint`
- Format: `pnpm format`
- Type check: `pnpm tsc --noEmit`
- Run locally: `pnpm start:dev`
- Migrations: `pnpm prisma migrate dev --name <name>` / `pnpm prisma migrate deploy`

## Project structure

```
src/
  <feature>/            # one Nest module per feature: controller, service, dto/, *.test.ts
  shared/               # prisma service, errors, auth guards
prisma/schema.prisma
test/                   # integration tests
```

- New features get their own module under `src/<feature>/`. Copy the structure of `src/[orders]/`.

## Coding conventions

- No `any`. Use `unknown` and narrow. No non-null assertions (`!`) to silence the compiler.
- Every promise is awaited or returned - no floating promises (`@typescript-eslint/no-floating-promises` is on). No `forEach(async ...)`.
- Expected failures return typed results or throw Nest `HttpException` subclasses at the controller boundary only; services don't know about HTTP.
- Prisma: `select` only the fields you need, no queries inside loops, always `take` on list queries.
- Config through `@nestjs/config` with a validated schema; never read `process.env` in business code.
- Request and response DTOs are separate from Prisma models; never return a Prisma model directly.

## Testing

- Unit tests for pure logic; integration tests with a real Postgres (Testcontainers) for anything touching Prisma.
- Don't mock Prisma for data-access tests.
- Bug fix = failing test first.

## Never do

- Commit `.env` files or secrets.
- Edit applied migrations in `prisma/migrations/`.
- Disable ESLint rules or add `// @ts-ignore` to make the build pass.
- Add a dependency without asking.

## Definition of done

- [ ] `pnpm build`, `pnpm lint`, `pnpm tsc --noEmit` pass with no new warnings.
- [ ] Tests for the new behavior exist and `pnpm test` passes.
- [ ] Migration created and reviewed if the schema changed.
- [ ] No leftover `console.log`, TODOs, or commented-out code.

## Workflow with Claude

- Plan first for non-trivial changes; wait for approval.
- Don't refactor code that isn't part of the task.
- Explain each change in one line (what + why).
