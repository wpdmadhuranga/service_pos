# Docker Guide for Service POS Backend

This project has been fully containerized with Docker and Docker Compose, targeting .NET 9 and PostgreSQL.

---

## Prerequisites

- [Docker Engine](https://docs.docker.com/engine/install/) (v20.10+)
- [Docker Compose](https://docs.docker.com/compose/) (v2+)

---

## Quick Start (Docker Compose)

### 1. (Optional) Customize Environment Variables
Copy `.env.example` to `.env` if you want to override defaults:
```bash
cp .env.example .env
```

### 2. Start Services
Run the following command to build the API image and start both the API and PostgreSQL containers:
```bash
docker compose up -d --build
```

### 3. Check Status and Logs
View running containers:
```bash
docker compose ps
```

View API container logs:
```bash
docker compose logs -f api
```

View Database container logs:
```bash
docker compose logs -f db
```

### 4. Access the API
- **API Base URL**: `http://localhost:5264`
- **OpenAPI Schema**: `http://localhost:5264/openapi/v1.json`
- **PostgreSQL Host Access**: `localhost:5433` (configurable, avoids port 5432 conflict if local PostgreSQL is installed)

### 5. Stop Services
Stop containers without removing database data:
```bash
docker compose down
```

To stop containers AND remove database volumes (fresh start):
```bash
docker compose down -v
```

---

## Docker Architecture

- **`api` service**:
  - Multi-stage build targeting .NET 9 (`mcr.microsoft.com/dotnet/aspnet:9.0` and `mcr.microsoft.com/dotnet/sdk:9.0`).
  - Runs under the non-root user (`app`, UID 1654) for container security.
  - Automatically waits for the PostgreSQL healthcheck before starting.
  - Automatic migration runner is enabled via `ApplyMigrationsAtStartup=true`.
  - Exposes port `8080` internally, mapped to host port `5264` by default.

- **`db` service**:
  - Image: `postgres:16-alpine`.
  - Health check: `pg_isready`.
  - Persistent volume: `postgres_data` ensures database records persist across container restarts.
  - Exposes port `5432` internally and on the host.

---

## Building and Running Docker Image Standalone

If you want to build and run only the API container with an external PostgreSQL instance:

```bash
# Build the image
docker build -t service-pos-api:latest .

# Run the container
docker run -d \
  -p 5264:8080 \
  -e ConnectionStrings__DefaultConnection="Host=<your-db-host>;Port=5432;Database=micro_service_1;Username=postgres;Password=<your-password>" \
  -e ASPNETCORE_ENVIRONMENT=Development \
  --name service-pos-api \
  service-pos-api:latest
```
