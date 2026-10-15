<!--
HOW TO USE THIS FILE
--------------------
Python example of the generic CLAUDE-md-template.md, filled in for a FastAPI + SQLAlchemy service managed with uv.
1. Copy everything BELOW the line into `CLAUDE.md` at your repo root.
2. Replace the [PLACEHOLDERS] and swap any tool you don't use (Django instead of FastAPI, Poetry instead of uv, mypy instead of pyright).
3. Delete rules that don't apply.
Made by Stefan Đokić · TheCodeMan · free to use and adapt.
==================================================================
-->

# Project: [PROJECT NAME]

[One or two sentences: what this service does.]

Instructions for Claude Code working in this repository. Follow these by default; ask before deviating.

## Stack

- **Python [3.12]**, fully type-hinted
- **Framework:** FastAPI with Pydantic v2
- **Data:** SQLAlchemy 2.0 (async) + Alembic, PostgreSQL
- **Testing:** pytest + pytest-asyncio + Testcontainers
- **Tooling:** uv, Ruff (lint + format), [mypy | pyright]

## Commands

- Install: `uv sync`
- Test (all): `uv run pytest`
- Test (single): `uv run pytest tests/path/test_file.py::test_name`
- Lint: `uv run ruff check .`
- Format: `uv run ruff format .`
- Type check: `uv run [mypy src | pyright]`
- Run locally: `uv run fastapi dev src/[app]/main.py`
- Migrations: `uv run alembic revision --autogenerate -m "<msg>"` / `uv run alembic upgrade head`

## Project structure

```
src/[app]/
  features/<feature>/   # router.py, schemas.py, service.py, models.py
  core/                 # settings, db session, auth, errors
tests/
alembic/
```

- New features go in `src/[app]/features/<feature>/`. Copy the structure of `features/[orders]/`.

## Coding conventions

- Type hints on every public function; no bare `Any` without a reason.
- `async def` endpoints use only async I/O. No `requests`, `time.sleep`, or sync DB sessions inside them; use `httpx.AsyncClient` and the async session.
- Pydantic schemas for every request and response; never return ORM objects directly.
- SQLAlchemy: eager-load with `selectinload`/`joinedload` explicitly, select only needed columns for reads, always paginate lists.
- Settings via `pydantic-settings`; never read `os.environ` in business code.
- Expected errors raise domain exceptions mapped to HTTP responses in one exception handler.

## Testing

- Integration tests run against a real Postgres via Testcontainers; no SQLite stand-in.
- Use fixtures for setup; no test depends on another test's state.
- Bug fix = failing test first.

## Never do

- Commit `.env` files or secrets.
- Edit applied Alembic migrations.
- Add `# type: ignore` or `# noqa` to silence errors without a comment explaining why.
- Add a dependency without asking.

## Definition of done

- [ ] `ruff check`, `ruff format --check`, and the type checker pass.
- [ ] Tests for the new behavior exist and `uv run pytest` passes.
- [ ] Alembic migration created and reviewed if models changed.
- [ ] No leftover `print`, TODOs, or commented-out code.

## Workflow with Claude

- Plan first for non-trivial changes; wait for approval.
- Don't refactor code that isn't part of the task.
- Explain each change in one line (what + why).
