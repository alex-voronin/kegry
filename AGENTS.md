# AGENTS.md

## Project structure

```
front/         Next.js 16 App Router (TypeScript, Tailwind v4, shadcn/ui)
back/          .NET 10 modular monolith (ASP.NET, EF Core, WolverineFx, PostgreSQL/PostGIS)
devops/        Docker Compose (9 services), SOPS secrets, deploy/provision scripts
docs/          Detailed engineering documentation
test-seed/     SQL seed scripts for local dev DB
```

## Documentation

- [`docs/frontend.md`](docs/frontend.md) — routing, i18n, auth/API, state, UI patterns, tests
- [`docs/backend.md`](docs/backend.md) — modules, startup, Wolverine, EF Core, API, tests
- [`docs/devops.md`](docs/devops.md) — Docker Compose, deployment, secrets, infrastructure

Read the relevant document before changing that part of the codebase.

## Key commands

All repository operations are defined in the root `Makefile`. Run `make help` for the full list.

```
make run-front              Dev server (localhost:3000)
make run-back               .NET dev server (localhost:5214)
make build                  Full build (both)
make run-tests              All backend tests (projects run sequentially)
make format                 Format backend and frontend
make add-migration-back     EF migration: module=X name=Y
make deploy                 Production deploy
make set-version            Bump frontend version + git tag: version=X.X.X
```

## Working rules

- Run `make format` and `make run-tests` before opening a pull request.
- Frontend checks: `cd front && npm run lint && npm test` (Vitest).
- Follow Conventional Commits (e.g. `feat(front): ...`, `fix(back): ...`).
- Keep changes consistent with the existing structure, naming, and patterns.

## OpenCode config

Local agents at `.opencode/agents/` and pipeline command at `.opencode/commands/pipeline.md` — uses `@planner → @plan-reviewer → @coder → @code-reviewer` flow.
