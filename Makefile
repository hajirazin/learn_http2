.PHONY: help db-up db-down db-restart db-logs db-shell db-clean backend-restore backend-build backend-run backend-migrate backend-migration backend-reset-db frontend-install frontend-dev frontend-build setup dev clean-all health

# Default target - show help
help:
	@echo "HTTP/2 Streaming Application - Development Commands"
	@echo ""
	@echo "Database (Docker) Commands:"
	@echo "  make db-up            - Start Postgres container"
	@echo "  make db-down          - Stop Postgres container"
	@echo "  make db-restart       - Restart Postgres container"
	@echo "  make db-logs          - View Postgres logs"
	@echo "  make db-shell         - Open psql shell in postgres container"
	@echo "  make db-clean         - Remove Postgres container and volumes"
	@echo ""
	@echo "Backend Commands (Local):"
	@echo "  make backend-restore  - Restore .NET dependencies"
	@echo "  make backend-build    - Build backend project"
	@echo "  make backend-run      - Run backend locally (port 5001)"
	@echo "  make backend-migrate  - Apply EF Core migrations"
	@echo "  make backend-migration NAME=<name> - Create new migration"
	@echo "  make backend-reset-db - Drop database and re-run migrations"
	@echo ""
	@echo "Frontend Commands (Local):"
	@echo "  make frontend-install - Install npm dependencies"
	@echo "  make frontend-dev     - Run frontend dev server (port 3000)"
	@echo "  make frontend-build   - Build frontend for production"
	@echo ""
	@echo "Combined Workflows:"
	@echo "  make setup            - First-time setup (db-up, restore deps, migrate)"
	@echo "  make dev              - Start DB (run backend/frontend manually in separate terminals)"
	@echo "  make clean-all        - Stop and clean everything"
	@echo "  make health           - Check health of all services"
	@echo ""

# ============================================================================
# Database (Docker) Commands
# ============================================================================

db-up:
	@echo "Starting Postgres container..."
	docker-compose up -d postgres
	@echo "Waiting for Postgres to be healthy..."
	@sleep 5
	@docker-compose ps postgres

db-down:
	@echo "Stopping Postgres container..."
	docker-compose down

db-restart:
	@echo "Restarting Postgres container..."
	docker-compose restart postgres

db-logs:
	@echo "Tailing Postgres logs (Ctrl+C to exit)..."
	docker-compose logs -f postgres

db-shell:
	@echo "Opening psql shell..."
	docker-compose exec postgres psql -U postgres -d http2streaming

db-clean:
	@echo "Removing Postgres container and volumes..."
	docker-compose down -v
	@echo "All data has been removed!"

# ============================================================================
# Backend Commands (Local)
# ============================================================================

backend-restore:
	@echo "Restoring .NET dependencies..."
	cd backend && dotnet restore

backend-build:
	@echo "Building backend project..."
	cd backend && dotnet build

backend-run:
	@echo "Running backend on https://localhost:5001..."
	@echo "Make sure Postgres is running (make db-up)"
	cd backend && dotnet run

backend-migrate:
	@echo "Applying EF Core migrations..."
	cd backend && dotnet ef database update

backend-migration:
	@if [ -z "$(NAME)" ]; then \
		echo "Error: Please provide NAME=<migration_name>"; \
		echo "Example: make backend-migration NAME=AddUserTable"; \
		exit 1; \
	fi
	@echo "Creating new migration: $(NAME)..."
	cd backend && dotnet ef migrations add $(NAME)

backend-reset-db:
	@echo "Dropping database and re-running migrations..."
	@echo "WARNING: This will delete all data!"
	@read -p "Are you sure? (y/N): " confirm; \
	if [ "$$confirm" = "y" ] || [ "$$confirm" = "Y" ]; then \
		cd backend && dotnet ef database drop -f && dotnet ef database update; \
		echo "Database has been reset!"; \
	else \
		echo "Cancelled."; \
	fi

# ============================================================================
# Frontend Commands (Local)
# ============================================================================

frontend-install:
	@echo "Installing npm dependencies..."
	cd frontend && npm install

frontend-dev:
	@echo "Running frontend dev server on http://localhost:3000..."
	@echo "Make sure backend is running (make backend-run)"
	cd frontend && npm run dev

frontend-build:
	@echo "Building frontend for production..."
	cd frontend && npm run build

# ============================================================================
# Combined Workflows
# ============================================================================

setup: db-up backend-restore frontend-install
	@echo ""
	@echo "Waiting for Postgres to be ready..."
	@sleep 5
	@echo "Running database migrations..."
	@$(MAKE) backend-migrate
	@echo ""
	@echo "Setup complete! 🎉"
	@echo ""
	@echo "Next steps:"
	@echo "  1. Start backend:  make backend-run  (in one terminal)"
	@echo "  2. Start frontend: make frontend-dev (in another terminal)"
	@echo ""

dev: db-up
	@echo ""
	@echo "Database started! ✅"
	@echo ""
	@echo "Now run these commands in separate terminals:"
	@echo "  Terminal 1: make backend-run"
	@echo "  Terminal 2: make frontend-dev"
	@echo ""

clean-all: db-clean
	@echo "Cleaning backend build artifacts..."
	cd backend && dotnet clean
	@echo "Cleaning frontend build artifacts..."
	cd frontend && rm -rf dist node_modules/.vite
	@echo "All cleaned! ✨"

health:
	@echo "Checking service health..."
	@echo ""
	@echo "Postgres:"
	@docker-compose ps postgres | grep -q "Up" && echo "  ✅ Running" || echo "  ❌ Not running"
	@echo ""
	@echo "Backend (expected on https://localhost:5001):"
	@curl -k -s https://localhost:5001 > /dev/null 2>&1 && echo "  ✅ Running" || echo "  ❌ Not running"
	@echo ""
	@echo "Frontend (expected on http://localhost:3000):"
	@curl -s http://localhost:3000 > /dev/null 2>&1 && echo "  ✅ Running" || echo "  ❌ Not running"
	@echo ""

