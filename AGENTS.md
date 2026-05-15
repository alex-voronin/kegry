# AGENTS.md — hoppyfy.me (Kegry)

## Project structure

```
front/         Next.js 16 App Router (TypeScript, Tailwind v4, shadcn/ui)
back/          .NET 10 modular monolith (ASP.NET, EF Core, WolverineFx, PostgreSQL/PostGIS)
devops/        Docker Compose (8 services), SOPS secrets, deploy/provision scripts
test-seed/     SQL seed scripts for local dev DB
landing-stub/  Static fallback landing page
```

## Frontend

- **Entrypoint:** `front/src/app/` — dual-platform via `b2c/` and `b2b/` sub-routers
- **Middleware:** `front/src/proxy.ts` — i18n (`next-intl`) + platform routing via subdomain (`business.` prefix → B2B)
- **i18n:** `next-intl`, locales `['en', 'sr']`, default `sr`, messages in `messages/{b2c,b2b}/{locale}.json`
- **State:** Zustand stores (`use-*-store.ts` pattern in `store/` and co-located with pages)
- **API client:** `src/lib/api-client.ts` — Axios with auto token refresh, retry on transient GET errors
- **Auth:** Firebase Auth via `react-firebase-hooks/auth`
- **Config gen:** `npm run generate-firebase-config` + `npm run generate-pwa-assets` run in `predev`/`prebuild`
- **Standalone build:** `output: 'standalone'` in next.config.ts
- **Dev:** `make run-front` (runs `cd front && npm run dev`)
- **Format:** `make format` — runs `eslint --fix` then `prettier --write` (single quotes, semicolons, prettier-plugin-tailwindcss)
- **No frontend tests** — only lint/format validation

## Backend

- **Entrypoint:** `back/API.Web/Program.cs` — registers `IFeatureStartup[]` for each module + cross-cutting concerns
- **Module layout** (12 modules in `back/Modules/`): `{Module}.API`, `.Application`, `.Domain`, `.Infrastructure`, `.Contracts`, `.Setup`
- **Message bus:** WolverineFx (handlers codegen: `make generate-handlers-back`, describe: `make describe-bus-back`)
- **DB:** EF Core + PostgreSQL (PostGIS) per module context. Migrations: `make add-migration-back module=X name=Y`
- **Tests:** xUnit + Shouldly + AutoFixture + NSubstitute
  - Unit tests: `make run-tests` (runs `dotnet test -m:1` — single-threaded to avoid flakiness)
  - Integration tests: using `WebApplicationFactory` + Testcontainers.PostgreSql + Respawn (DB reset per test)
- **Dev server:** `make run-back` (port 5214)
- **Build:** `make build-back` — `dotnet build` on the solution

## DevOps

- **Deploy:** `make deploy` — builds Docker images, transfers via rsync to remote host, deploys via docker-compose
- **Provision:** `make provision` — initial server setup
- **Secrets:** SOPS with SSH key encryption. Encrypt: `make dotenv-encrypt`, decrypt: `make dotenv-decrypt`
- **Infra:** Nginx reverse proxy → Cloudflare Tunnel, VictoriaMetrics (Grafana Cloud), PostgreSQL, S3-compatible R2 for images

## Key commands summary

```
make run-front              Dev server (localhost:3000)
make run-back               .NET dev server (localhost:5214)
make build                  Full build (both)
make run-tests              All backend tests (single-threaded)
make format                 Lint + format (both sides)
make add-migration-back     EF migration: module=X name=Y
make deploy                 Production deploy
make set-version            Bump frontend version + git tag: version=X.X.X
```

## OpenCode config

Local agents at `.opencode/agents/` and pipeline command at `.opencode/commands/pipeline.md` — uses `@planner → @plan-reviewer → @coder → @code-reviewer` flow.
