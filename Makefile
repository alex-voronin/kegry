.PHONY: build-back build-front run-back describe-bus-back add-migration-back list-modules-back run-front run-tests deploy format audit db-shell clean help

# Default target
help:
	@echo "Development Commands:"
	@echo "  make build-back  - Build the .NET backend solution"
	@echo "  make build-front - Install dependencies and build the Next.js frontend"
	@echo "  make run-back    - Run the .NET backend API (API.Web)"
	@echo "  make describe-bus-back - Describe WolverineFX bus routes"
	@echo "  make run-front   - Run the Next.js frontend in development mode"
	@echo "  make run-tests   - Run all backend tests (Unit, Integration)"
	@echo "  make format      - Run linters and format code (dotnet format, eslint, prettier)"
	@echo "  make audit       - Runs security audit for both backend and frontend"
	@echo "  make clean       - Deep clean build artifacts (bin, obj, .next)"
	@echo "  make deploy      - Build and deploy the produciton build"
	@echo "  make add-migration-back module=<Module> name=<Name> - Add EF migration for a module"
	@echo "  make list-modules-back - List all backend modules"

# Backend commands
build-back:
	dotnet build back/hoppyfy.me.sln

run-back:
	dotnet run --project back/API.Web/API.Web.csproj

describe-bus-back:
	dotnet run --project back/API.Web/API.Web.csproj -- describe

run-tests:
	dotnet test back/hoppyfy.me.sln -m:1

format-back:
	dotnet format back/hoppyfy.me.sln

audit-back:
	dotnet list back/hoppyfy.me.sln package --vulnerable

list-modules-back:
	@ls -d back/Modules/*/ | xargs -n 1 basename

add-migration-back:
	@if [ -z "$(module)" ] || [ -z "$(name)" ]; then \
		echo "Usage: make add-migration-back module=<ModuleName> name=<MigrationName>"; \
		echo "Example: make add-migration-back module=Security name=AddSecurity"; \
		exit 1; \
	fi
	dotnet ef migrations add $(name) \
		--project back/Modules/$(module)/$(module)Module.Infrastructure \
		--startup-project back/API.Web/API.Web.csproj \
		--output-dir Persistence/Migrations \
		--context $(module)Context

# Frontend commands
build-front:
	cd front && npm install && npm run build

run-front:
	cd front && npm run dev

format-front:
	cd front && npm run lint -- --fix && npx prettier --write .

audit-front:
	cd front && npm audit

# Combined commands
format: format-back format-front

audit: audit-back audit-front

clean:
	@echo "Cleaning build artifacts..."
	find . -type d -name "bin" -exec rm -rf {} +
	find . -type d -name "obj" -exec rm -rf {} +
	rm -rf front/.next
	@echo "Clean complete."

# Deployment
deploy:
	./devops/deploy.sh
