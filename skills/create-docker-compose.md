# Skill: Docker / docker-compose for ArbiScannerAdminPannel

## Project structure

```
Projects/
├── ArbiScannerAdminPannel/     ← this workspace (ASP.NET API + React admin panel)
│   ├── Dockerfile              ← .NET API image (server only)
│   ├── Dockerfile.client       ← React + Nginx image (client only)
│   ├── nginx.conf              ← Nginx config: SPA + /api/ proxy to api container
│   ├── docker-compose.yml      ← full stack (client + api + postgres + redis)
│   ├── .dockerignore
│   ├── ArbiScannerAdminPannel.API/
│   ├── ArbiScannerAdminPannel.Core/
│   ├── ArbiScannerAdminPannel.Domain/
│   ├── ArbiScannerAdminPannel.Infrastructure/
│   └── ArbiScannerAdminPanel.Client/  ← Vite + React SPA
└── ArbiScannerWebApp/              ← sibling project referenced by the server
    ├── ArbiScannerWeb.Business/
    └── ArbiScannerWeb.Infrastructure/
```

## Architecture: two containers

| Container  | Image              | What it does                                          |
|------------|--------------------|-------------------------------------------------------|
| `client`   | `Dockerfile.client`| Nginx serves Vite-built React, proxies `/api/` → api  |
| `api`      | `Dockerfile`       | ASP.NET API on port 8080                              |

The React SPA is **not** bundled into the .NET image. The `.esproj` has
`<ShouldRunBuildScript>false</ShouldRunBuildScript>`, so `dotnet publish` never
runs `npm`. The client is built in its own Docker stage.

Traffic flow: browser → `localhost:3000` (Nginx) → `/api/*` → `api:8080` (.NET)

## Why the API build context must be the Projects parent

`ArbiScannerAdminPannel.API.csproj` and `ArbiScannerAdminPannel.Infrastructure.csproj`
both have `<ProjectReference>` entries pointing to `../../ArbiScannerWebApp/…`,
which lives **outside** the workspace root. Docker can only access files within
the build context, so the API build context is set one level up (`Projects/`).

The client build context is just `./` (workspace root) — it only needs the
`ArbiScannerAdminPanel.Client/` folder and `nginx.conf`.

## Building images individually

```bash
# API image — context is the Projects/ parent
cd /path/to/Projects
docker build -f ArbiScannerAdminPannel/Dockerfile -t arbiscanner-admin-api .

# Client image — context is the workspace root
cd /path/to/Projects/ArbiScannerAdminPannel
docker build -f Dockerfile.client -t arbiscanner-admin-client .
```

## Running with docker compose (recommended)

The `docker-compose.yml` starts the full stack:

| Service    | Port(s) | Purpose                         |
|------------|---------|---------------------------------|
| `client`   | 3000    | Nginx + React SPA (entry point) |
| `api`      | 8080    | ASP.NET API                     |
| `postgres` | 5432    | PostgreSQL 17 (two databases)   |
| `redis`    | 6379    | Redis 7                         |

Two PostgreSQL databases are auto-created from shared credentials:
- `ArbiScannerBot` — main app data (`DefaultConnection`)
- `ArbiScannerAdminPanelDb` — admin panel data (`AdminConnection`)

```bash
# Run from the Projects parent directory
cd /path/to/Projects
docker compose -f ArbiScannerAdminPannel/docker-compose.yml up --build

# Detached (background)
docker compose -f ArbiScannerAdminPannel/docker-compose.yml up -d --build

# Stop & remove containers
docker compose -f ArbiScannerAdminPannel/docker-compose.yml down

# Remove containers AND volumes (full reset)
docker compose -f ArbiScannerAdminPannel/docker-compose.yml down -v
```

## Configuration overrides via environment variables

```yaml
# API service
ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=ArbiScannerBot;Username=postgres;Password=REDACTED"
ConnectionStrings__AdminConnection: "Host=postgres;Port=5432;Database=ArbiScannerAdminPanelDb;Username=postgres;Password=REDACTED"
Redis__Endpoint: "redis:6379"
Cors__AllowedOrigins: "http://localhost:3000"   # comma-separated for multiple origins
```

CORS origins are read in `Program.cs`:
```csharp
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
```

Redis endpoint is read via:
```csharp
builder.Configuration["Redis:Endpoint"] ?? "localhost"
```

## Dockerfile stages explained

### `Dockerfile` (API)
| Stage       | Base image               | Purpose                          |
|-------------|--------------------------|----------------------------------|
| `build-env` | `dotnet/sdk:10.0`        | Restore NuGet + publish          |
| `final`     | `dotnet/aspnet:10.0`     | Lean runtime image               |

### `Dockerfile.client` (React)
| Stage   | Base image        | Purpose                                        |
|---------|-------------------|------------------------------------------------|
| `build` | `node:20-alpine`  | `npm ci` + `npm run build` → Vite dist output  |
| `final` | `nginx:alpine`    | Serve static files + proxy `/api/` to api      |

## Adding a new service to docker-compose

1. Add a new entry under `services:` with image, ports, env, and networks.
2. Add it to `depends_on:` of `client` or `api` as needed.
3. Declare a named volume if the service needs persistence.
4. Attach to `backend` network for inter-service communication.

## Migrations

Run EF Core migrations against the containerised Postgres:

```bash
# Admin panel DB (while the stack is up)
dotnet ef database update \
  --project ArbiScannerAdminPannel.Infrastructure \
  --startup-project ArbiScannerAdminPannel.API \
  --context AdminPanelAppDbContext \
  -- --connectionstrings:adminconnection "Host=localhost;Port=5432;Database=ArbiScannerAdminPanelDb;Username=postgres;Password=REDACTED"
```
