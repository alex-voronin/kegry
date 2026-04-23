.PHONY: help build build-back build-front run-back describe-bus-back \
        add-migration-back list-modules-back run-front run-tests \
        format audit deploy provision db-shell clean \
        dotenv-setup-encryption dotenv-encrypt dotenv-decrypt \
        dotenv-encrypt-file dotenv-decrypt-file dotenv-cleanup \
        get-version set-version

# =============================================
# Configuration
# =============================================
SHELL := /bin/bash
.DEFAULT_GOAL := help

BACK_DIR        := back
FRONT_DIR       := front
BACK_SOLUTION   := $(BACK_DIR)/hoppyfy.me.sln
WEB_API_PROJECT := $(BACK_DIR)/API.Web/API.Web.csproj

# Colors for nice help output
BLUE  := \033[36m
GREEN := \033[32m
RESET := \033[0m
BOLD  := \033[1m

# =============================================
# Main commands
# =============================================

help: ## Show all available commands
	@echo -e "${BOLD}Available commands:${RESET}"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | \
		awk 'BEGIN {FS = ":.*?## "}; \
		{printf "  ${BLUE}%-25s${RESET} %s\n", $$1, $$2}' | sort

build: build-back build-front ## Build both backend and frontend

# =============================================
# Backend
# =============================================

build-back: ## Build the .NET backend solution
	dotnet build $(BACK_SOLUTION)

run-back: ## Run the .NET backend API
	dotnet run --project $(WEB_API_PROJECT)

describe-bus-back: ## Describe WolverineFX bus routes
	dotnet run --project $(WEB_API_PROJECT) -- describe

run-tests: ## Run all backend tests
	dotnet test $(BACK_SOLUTION) -m:1

list-modules-back: ## List all backend modules
	@ls -d $(BACK_DIR)/Modules/*/ | xargs -n 1 basename

add-migration-back: ## Add EF Core migration. Usage: make add-migration-back module=Security name=AddSecurity
	@if [ -z "$(module)" ] || [ -z "$(name)" ]; then \
		echo "Error: module and name parameters are required"; \
		echo "Usage: make add-migration-back module=<ModuleName> name=<MigrationName>"; \
		echo "Example: make add-migration-back module=Security name=AddSecurity"; \
		exit 1; \
	fi
	dotnet ef migrations add $(name) \
		--project $(BACK_DIR)/Modules/$(module)/$(module)Module.Infrastructure \
		--startup-project $(WEB_API_PROJECT) \
		--output-dir Persistence/Migrations \
		--context $(module)Context

# =============================================
# Frontend
# =============================================

build-front: ## Build the Next.js frontend
	cd $(FRONT_DIR) && npm install && npm run build

build-front-standalone: ## Build the Next.js frontend and copy static resources to standalone folder
	cd $(FRONT_DIR) && npm install && npm run build && npm run prepare:standalone

run-front: ## Run the Next.js frontend in development mode
	cd $(FRONT_DIR) && npm run dev

# =============================================
# Combined commands
# =============================================

format: ## Format code (backend + frontend)
	@$(MAKE) format-back
	@$(MAKE) format-front

audit: ## Run security audit (backend + frontend)
	@$(MAKE) audit-back
	@$(MAKE) audit-front

clean: ## Deep clean build artifacts
	@echo "Cleaning .NET artifacts..."
	find $(BACK_DIR) -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} + 2>/dev/null || true
	@echo "Cleaning Next.js artifacts..."
	rm -rf $(FRONT_DIR)/.next 2>/dev/null || true
	@echo "Clean complete."

# =============================================
# Deployment
# =============================================

provision: ## Initialize the remote server (install all required software)
	./devops/provision.sh

deploy: ## Build and deploy the production build
	./devops/deploy.sh

forward-prod-db: ## Forward remote PostgreSQL port to local 54321
	@echo "The PostgreSQL database is now available locally on port 54321."
	ssh -L 54321:localhost:5432 kegry-host

# =============================================
# Secrets Management
# =============================================

dotenv-setup-encryption: ## Initialize SOPS configuration using your SSH key
	./devops/sops-init.sh

dotenv-encrypt: ## Encrypt all .env.* files into .env.*.sops
	./devops/secrets.sh encrypt

dotenv-decrypt: ## Decrypt all .env.*.sops files into .env.*
	./devops/secrets.sh decrypt

dotenv-encrypt-file: ## Encrypt a specific .env file. Usage: make dotenv-encrypt-file file=devops/configs/.env.back
	@if [ -z "$(file)" ]; then echo "Error: file parameter is required"; exit 1; fi
	./devops/secrets.sh encrypt $(file)

dotenv-decrypt-file: ## Decrypt a specific .sops file. Usage: make dotenv-decrypt-file file=devops/configs/.env.back.sops
	@if [ -z "$(file)" ]; then echo "Error: file parameter is required"; exit 1; fi
	./devops/secrets.sh decrypt $(file)

dotenv-cleanup: ## Remove decrypted .env files that have a .sops source
	./devops/clean-dotenv.sh

# =============================================
# Versioning
# =============================================

get-version: ## Get current frontend version
	@npm pkg get version --prefix $(FRONT_DIR) | tr -d '\"'

set-version: ## Set frontend version and create git tag. Usage: make set-version version=X.X.X
	@if [ -z "$(version)" ]; then \
		echo "Error: version parameter is required"; \
		echo "Usage: make set-version version=1.2.3"; \
		exit 1; \
	fi
	@if [ -n "$$(git status --porcelain)" ]; then \
		echo "Error: working directory is dirty. Commit or stash all changes first"; \
		exit 1; \
	fi
	@if git rev-parse "v$(version)" >/dev/null 2>&1; then \
		echo "Error: tag v$(version) already exists"; \
		exit 1; \
	fi

	cd $(FRONT_DIR) && npm version $(version) --no-git-tag-version && \
	git add package.json package-lock.json && \
	git commit -m "chore: release v$(version)" && \
	git tag "v$(version)"

	@echo "Version set to $(version) and tagged."


# =============================================
# Helper targets
# =============================================

format-back:
	dotnet format $(BACK_SOLUTION)

format-front:
	cd $(FRONT_DIR) && npm run lint -- --fix && npx prettier --write .

audit-back:
	dotnet list $(BACK_SOLUTION) package --vulnerable

audit-front:
	cd $(FRONT_DIR) && npm audit

db-shell:
	@echo "TODO: configure db-shell for your database (psql, dotnet ef, etc.)"