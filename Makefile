.PHONY: build-back build-front run-back run-front run-tests deploy format db-shell clean help

# Default target
help:
	@echo "Development Commands:"
	@echo "  make build-back  - Build the .NET backend solution"
	@echo "  make build-front - Install dependencies and build the Next.js frontend"
	@echo "  make run-back    - Run the .NET backend API (API.Web)"
	@echo "  make run-front   - Run the Next.js frontend in development mode"
	@echo "  make run-tests   - Run all backend tests (Unit, Integration)"
	@echo "  make format      - Run linters and format code (dotnet format, eslint, prettier)"
	@echo "  make clean       - Deep clean build artifacts (bin, obj, .next)"
	@echo "  make deploy      - Build and deploy the produciton build"

# Backend commands
build-back:
	dotnet build back/hoppyfy.me.sln

run-back:
	dotnet run --project back/API.Web/API.Web.csproj

run-tests:
	dotnet test back/hoppyfy.me.sln -m:1

format-back:
	dotnet format back/hoppyfy.me.sln

# Frontend commands
build-front:
	cd front && npm install && npm run build

run-front:
	cd front && npm run dev

format-front:
	cd front && npm run lint -- --fix && npx prettier --write .

# Combined commands
format: format-back format-front

clean:
	@echo "Cleaning build artifacts..."
	find . -type d -name "bin" -exec rm -rf {} +
	find . -type d -name "obj" -exec rm -rf {} +
	rm -rf front/.next
	@echo "Clean complete."

# Deployment
deploy:
	./devops/deploy.sh
