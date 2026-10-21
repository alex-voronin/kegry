# DevOps

## Services

`devops/docker-compose.yml` defines 9 services:

- `postgres` — PostgreSQL 18 with PostGIS
- `backend` — .NET API
- `frontend` — Next.js standalone server
- `nginx` — reverse proxy
- `cloudflared` — Cloudflare Tunnel
- `vmagent` — metrics agent for Grafana Cloud
- `node-exporter` — host metrics
- `db-backup` — scheduled PostgreSQL backups to S3-compatible storage
- `db-backup-retention` — local backup retention (35 days + one per month)

## Deployment

- `make provision` — initial server setup.
- `make deploy` — decrypts SOPS secrets if needed, builds `linux/amd64` images, transfers and loads them on the server, and runs `docker compose up -d`.
- `make forward-prod-db` — forwards the remote PostgreSQL port to local `54321`.

Deployment configuration lives in `devops/.env.deploy` (see `devops/.env.deploy.example`).

## Secrets

Secrets are encrypted with SOPS using an age key backed by an SSH Ed25519 key (`.sops.yaml`).

- `make dotenv-setup-encryption` — initialize SOPS with your SSH key.
- `make dotenv-decrypt` / `make dotenv-encrypt` — decrypt or encrypt all `.env.*` files.
- `make dotenv-decrypt-file file=...` / `make dotenv-encrypt-file file=...` — process a single file.
- `make dotenv-cleanup` — remove decrypted files that have a `.sops` source.

Decrypted files are gitignored. Templates are available as `devops/.env.*.example` and `devops/configs/.env.*.example`.

## Infrastructure

- Nginx sits in front of the backend and frontend; Cloudflare Tunnel provides ingress.
- `vmagent` and `node-exporter` send metrics to Grafana Cloud.
- Images are stored in S3-compatible storage (Cloudflare R2 in production).
- PostgreSQL backups are uploaded to S3-compatible storage.

## Versioning

- `make get-version` — print the frontend version.
- `make set-version version=X.Y.Z` — update `front/package.json`, commit the change, and create a `vX.Y.Z` tag. The working tree must be clean.
