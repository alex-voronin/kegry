# Bar Growth Platform

A platform for bars and their guests, consisting of two applications that share one API:

- **B2C app** — venue discovery, meetups, chat, loyalty programs, and user profiles.
- **B2B portal** — company and venue management, promotions, discounts, loyalty levels, events, and dashboards. Served on the `business.` subdomain.

## Tech stack

- **Frontend:** Next.js 16 (App Router), React 19, TypeScript, Tailwind CSS v4, Zustand, next-intl
- **Backend:** .NET 10, ASP.NET Core minimal APIs, WolverineFX, EF Core, PostgreSQL + PostGIS
- **Infrastructure:** Docker Compose, Nginx, Cloudflare Tunnel, SOPS

## Repository layout

```
front/         Next.js app (B2C + B2B)
back/          .NET modular monolith (API, modules, tests)
devops/        Docker Compose, Nginx, Cloudflare, deployment scripts
docs/          Engineering documentation
test-seed/     SQL seed scripts for local development
```

## Repository commands

All repository operations are defined in the root `Makefile`. Run `make help` to list every available target.

| Command                                               | Description                                   |
| ----------------------------------------------------- | --------------------------------------------- |
| `make build`                                          | Build backend and frontend                    |
| `make run-back`                                       | Run the API                                   |
| `make run-front`                                      | Run the Next.js dev server                    |
| `make run-tests`                                      | Run backend tests                             |
| `make format`                                         | Format backend and frontend                   |
| `make audit`                                          | Check dependencies for vulnerabilities        |
| `make add-migration-back module=<Module> name=<Name>` | Add an EF Core migration                      |
| `make provision`                                      | Initialize a fresh server                     |
| `make deploy`                                         | Build and deploy to production                |
| `make dotenv-decrypt` / `make dotenv-encrypt`         | Manage SOPS-encrypted secrets                 |
| `make set-version version=X.Y.Z`                      | Set the frontend version and create a git tag |

## Documentation

- [Frontend](docs/frontend.md)
- [Backend](docs/backend.md)
- [DevOps](docs/devops.md)
