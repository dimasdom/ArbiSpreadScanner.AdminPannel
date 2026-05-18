# ArbiScannerAdminPanel

Admin and manager panel for the ArbiScanner platform. Provides administrators and managers with tools to view and manage users, handle subscriptions, process crypto payments via OxaPay, configure system settings, and monitor platform activity. This is a separate application from the user-facing ArbiScannerWebApp.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Technologies](#technologies)
- [Two-Database Setup](#two-database-setup)
- [Prerequisites](#prerequisites)
- [Running Locally](#running-locally)
- [Configuration](#configuration)
- [Environment Variables (Docker)](#environment-variables-docker)
- [Docker Build](#docker-build)
- [Database Migrations](#database-migrations)
- [Seeding Initial Users](#seeding-initial-users)
- [Project Structure](#project-structure)

---

## Overview

ArbiScannerAdminPanel is a full-stack application consisting of an ASP.NET Core 10 REST API and a React 19 SPA. It is deployed as a git submodule within the ArbiScanner monorepo.

Key capabilities:
- User management (view users from the shared ArbiScannerBot database)
- Subscription management (create, assign, and revoke subscriptions)
- Payment processing via OxaPay (crypto payment gateway)
- JWT-based authentication with access and refresh tokens for admin/manager roles
- Real-time activity monitoring via SignalR
- Structured logging via Serilog, shipped to Grafana Loki

**Note on folder naming:** The git submodule directory is named `ArbiScannerAdminPannel` (double-n) because renaming a submodule root requires re-registering it in `.gitmodules`. All internal project names and namespaces use the corrected spelling `ArbiScannerAdminPanel` (single-n).

---

## Architecture

The solution follows Clean Architecture, organized into six projects with strict dependency rules flowing inward.

### ArbiScannerAdminPanel.Domain

The innermost layer. Contains all domain models, identity entities, and value objects. Notable types:

- `AdminUserModel` — extends ASP.NET Core Identity's `IdentityUser`; represents admin and manager accounts
- `AdminRefreshTokenModel` — stores hashed refresh tokens with rotation and revocation support
- `SubscriptionModel` — defines subscription tiers (type, price, duration)
- `UserSubscriptionModel` — tracks which platform user holds which subscription and its validity period
- `PaymentModel` — records individual payment transactions
- `UserSubscriptionPayment` — join entity linking a payment to a subscription activation
- `JwtOptions` — typed configuration for JWT settings

Has no dependencies on other projects in this solution.

### ArbiScannerAdminPanel.Abstractions

Defines interfaces for all services and repositories. References Domain only. Nothing in this layer holds any implementation.

Service interfaces: `IAccountService`, `IAdminService`, `IOxaPayService`, `IPaymentsService`, `ISubscriptionService`, `IUsersService`

Repository interfaces: `IAdminRefreshTokenRepository`, `IAdminUsersRepository`, `IPaymentsRepository`, `ISubscriptionsRepository`, `IWebAppUserRepository`

Key package: `FluentResults` — all service interfaces return `Result<T>` or `Result`, making error handling explicit without exceptions crossing layer boundaries.

### ArbiScannerAdminPanel.Application

Implements the service interfaces defined in Abstractions. Contains all business logic: account registration and authentication, subscription assignment, payment lifecycle, OxaPay invoice creation and webhook handling.

References: Abstractions + Domain.

Services:
- `AccountService` — handles login, token issuance, refresh token rotation, and logout
- `UsersService` — queries platform user data from the shared WebApp database
- `SubscriptionService` — manages subscription plan definitions and user subscription assignments
- `PaymentsService` — orchestrates payment records and links them to subscription activations
- `OxaPayService` — wraps the OxaPay HTTP API for invoice creation and status checks

### ArbiScannerAdminPanel.Infrastructure

Implements repositories and provides all external integrations. References Application (and transitively Abstractions + Domain).

- `AdminPanelAppDbContext` — `IdentityDbContext<AdminUserModel>` that owns the admin-only schema: admin users, roles, refresh tokens, subscriptions, payments, and subscription-payment join records. Backed by the `ArbiScannerAdminPanelDb` PostgreSQL database.
- `AppDbContext` (from `ArbiScannerWebApp.Infrastructure`) — read-only access to the shared `ArbiScannerBot` PostgreSQL database used by the WebApp and Telegram bot.
- Redis — used for caching via `StackExchange.Redis` and `IConnectionMultiplexer`.
- Repository implementations for all five repository interfaces.

Key packages: EF Core 10, Npgsql 10, StackExchange.Redis, FluentResults.

### ArbiScannerAdminPanel.API

ASP.NET Core 10 Web API. The composition root: wires up all services, middleware, and infrastructure.

Controllers:
- `AccountController` — login, refresh, logout endpoints
- `UsersController` — list and inspect platform users
- `SubscriptionsController` — subscription plan CRUD and user subscription management
- `PaymentsController` — payment records and OxaPay webhook receiver

Additional features:
- JWT Bearer authentication (access token: 15 min, refresh token: 7 days)
- CORS policy controlled by configuration
- OpenAPI (Scalar) — available in development at `/openapi`
- Serilog request logging, enriched with machine name and thread ID, shipped to Grafana Loki
- Global exception handling middleware (`ExceptionHandlingMiddleware`)
- On startup: ensures roles exist, seeds default subscription tiers, and optionally seeds admin/manager users (see [Seeding Initial Users](#seeding-initial-users))

### ArbiScannerAdminPanel.Client

React 19 + Vite + TypeScript SPA. Communicates with the API over HTTP. The API base URL is supplied at build time via the `VITE_API_URL` environment variable.

UI: HeroUI + MUI (Material UI v7, MUI X Data Grid) + Tailwind CSS v4 + Framer Motion  
State: Redux Toolkit + redux-persist + React Redux  
Charts: ApexCharts + react-apexcharts  
Real-time: `@microsoft/signalr`  
HTTP: axios  
Routing: react-router v7

---

## Technologies

| Layer | Technology |
|---|---|
| Runtime | .NET 10, ASP.NET Core 10 |
| ORM | Entity Framework Core 10 |
| Database driver | Npgsql 10 (PostgreSQL) |
| Identity | ASP.NET Core Identity |
| Auth | JWT Bearer (access + refresh tokens) |
| Cache | Redis via StackExchange.Redis |
| Payments | OxaPay crypto payment gateway |
| Logging | Serilog → Grafana Loki |
| API docs | Scalar / OpenAPI |
| Error handling | FluentResults |
| Frontend runtime | Node 20, React 19, Vite 6, TypeScript 5.8 |
| UI | HeroUI, MUI v7, Tailwind CSS v4 |
| State | Redux Toolkit, redux-persist |
| Charts | ApexCharts |
| Real-time | SignalR (@microsoft/signalr) |
| Containerization | Docker (multi-stage), nginx |

---

## Two-Database Setup

The application connects to two PostgreSQL databases simultaneously.

**`ArbiScannerBot` (DefaultConnection)**
Shared with ArbiScannerWebApp and the Telegram bot. This database holds all platform user accounts and spread/arbitrage data. The admin panel reads from this database (via `AppDbContext` and `WebAppUserRepository`) to display user information but does not own or migrate this schema.

**`ArbiScannerAdminPanelDb` (AdminConnection)**
Admin-only database owned entirely by this application. Contains:
- Admin and manager user accounts (ASP.NET Identity tables)
- Role definitions (`Administrator`, `Manager`)
- Refresh token records
- Subscription plan definitions
- User subscription assignments
- Payment records

EF Core migrations in `ArbiScannerAdminPanel.Infrastructure/Migrations/` target `AdminPanelAppDbContext` and apply only to `ArbiScannerAdminPanelDb`.

---

## Prerequisites

- .NET SDK 10.0+
- Node.js 20+ and npm
- PostgreSQL 17 (two databases: `ArbiScannerBot` and `ArbiScannerAdminPanelDb`)
- Redis 7
- (Optional) Docker and Docker Compose for containerized deployment

---

## Running Locally

### 1. Clone with submodules

If you are cloning the ArbiScanner monorepo for the first time:

```bash
git clone --recurse-submodules <repo-url>
```

Or, if already cloned:

```bash
git submodule update --init --recursive
```

### 2. Configure the API

Copy or edit `ArbiScannerAdminPanel.API/appsettings.json` (or create `appsettings.Development.json`) and fill in your local values. See [Configuration](#configuration) for all keys.

At minimum, set:
- `ConnectionStrings:DefaultConnection` — PostgreSQL connection to `ArbiScannerBot`
- `ConnectionStrings:AdminConnection` — PostgreSQL connection to `ArbiScannerAdminPanelDb`
- `Jwt:SigningKey` — a long random string (at least 32 characters)
- `Redis:Endpoint` — e.g. `localhost:6379`

### 3. Apply database migrations

```bash
cd ArbiScannerAdminPannel/ArbiScannerAdminPanel.API

dotnet ef database update --context AdminPanelAppDbContext
```

The shared `ArbiScannerBot` database is managed by ArbiScannerWebApp. Run its migrations separately if the database does not already exist.

### 4. Seed the initial admin user

On first run, set `Seed:Enabled` to `true` in your local `appsettings.Development.json` and provide credentials:

```json
{
  "Seed": {
    "Enabled": true,
    "AdminUserName": "admin",
    "AdminPassword": "YourStrongPassword1!",
    "ManagerUserName": "manager",
    "ManagerPassword": "YourStrongPassword2!"
  }
}
```

After the first successful startup, set `Seed:Enabled` back to `false`. The seeder is idempotent and will not duplicate existing users.

### 5. Start the API

```bash
cd ArbiScannerAdminPannel/ArbiScannerAdminPanel.API
dotnet run
```

The API starts at `http://localhost:5046`. In development mode, OpenAPI documentation is available at `http://localhost:5046/openapi`.

### 6. Start the React client

```bash
cd ArbiScannerAdminPannel/ArbiScannerAdminPanel.Client
npm install
npm run dev
```

The client starts at `http://localhost:5174` and expects the API at the URL configured in `VITE_API_URL` (defaults to `http://localhost:5046` during development via Vite proxy or direct axios base URL).

---

## Configuration

All settings live in `ArbiScannerAdminPanel.API/appsettings.json`. Override per-environment in `appsettings.Development.json` or via environment variables (Docker).

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ArbiScannerBot;Username=postgres;Password=...",
    "AdminConnection": "Host=localhost;Port=5432;Database=ArbiScannerAdminPanelDb;Username=postgres;Password=..."
  },
  "Jwt": {
    "Issuer": "ArbiScannerAdminPanel",
    "Audience": "ArbiScannerAdminPanel.Client",
    "SigningKey": "...",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Cors": {
    "AllowedOrigins": "http://localhost:5174"
  },
  "Redis": {
    "Endpoint": "localhost:6379"
  },
  "Seed": {
    "Enabled": false,
    "AdminUserName": "",
    "AdminPassword": "",
    "ManagerUserName": "",
    "ManagerPassword": ""
  },
  "OxaPay": {
    "BaseUrl": "https://api.oxapay.com/v1",
    "MerchantApiKey": "...",
    "DefaultCurrency": "USD",
    "DefaultLifetime": 30,
    "Sandbox": true
  }
}
```

`OxaPay:Sandbox` — set to `true` during development/testing to use the OxaPay sandbox environment. Set to `false` in production.

`Cors:AllowedOrigins` — accepts a comma-separated list of origins when multiple are needed.

---

## Environment Variables (Docker)

When running via Docker, all sensitive and environment-specific values are supplied as environment variables. ASP.NET Core maps `__` to `:` in configuration keys automatically.

| Variable | Maps to | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` | PostgreSQL connection to ArbiScannerBot |
| `ConnectionStrings__AdminConnection` | `ConnectionStrings:AdminConnection` | PostgreSQL connection to ArbiScannerAdminPanelDb |
| `JWT_SIGNING_KEY_ADMINPANEL` | `Jwt:SigningKey` | JWT signing secret (keep long and random) |
| `JWT_ISSUER_ADMINPANEL` | `Jwt:Issuer` | JWT issuer claim |
| `JWT_AUDIENCE_ADMINPANEL` | `Jwt:Audience` | JWT audience claim |
| `ADMIN_CLIENT_URL` | `Cors:AllowedOrigins` | CORS allowed origin for the React client |
| `Redis__Endpoint` | `Redis:Endpoint` | Redis connection string, e.g. `redis:6379` |
| `SEED_ENABLED` | `Seed:Enabled` | Set to `true` on first deploy only |
| `ADMIN_USERNAME` | `Seed:AdminUserName` | Initial admin account username |
| `ADMIN_PASSWORD` | `Seed:AdminPassword` | Initial admin account password |
| `MANAGER_USERNAME` | `Seed:ManagerUserName` | Initial manager account username |
| `MANAGER_PASSWORD` | `Seed:ManagerPassword` | Initial manager account password |
| `OXAPAY_BASE_URL` | `OxaPay:BaseUrl` | OxaPay API base URL |
| `OXAPAY_MERCHANT_API_KEY` | `OxaPay:MerchantApiKey` | OxaPay merchant key |
| `OXAPAY_DEFAULT_CURRENCY` | `OxaPay:DefaultCurrency` | Default invoice currency |
| `OXAPAY_DEFAULT_LIFETIME` | `OxaPay:DefaultLifetime` | Invoice lifetime in minutes |
| `OXAPAY_SANDBOX` | `OxaPay:Sandbox` | `true` for sandbox mode, `false` for production |

**React client build argument:**

| Variable | Description |
|---|---|
| `VITE_API_URL` | Admin API base URL baked into the client bundle, e.g. `http://ip:8081` |

---

## Docker Build

The project uses two Dockerfiles. The build context for the API Dockerfile must be the monorepo root (`ArbiScanner/`) because the API project references code from the sibling `ArbiScannerWebApp/` directory.

### API

- **Dockerfile:** `ArbiScannerAdminPannel/Dockerfile`
- **Build context:** monorepo root (`../` relative to the submodule)
- **Exposed port:** 8080 (mapped to host port 8081 in production deployments)
- **Base images:** `mcr.microsoft.com/dotnet/sdk:10.0` (build) → `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime)

The `.esproj` reference to the React client is stripped at build time using `sed` so that `dotnet publish` does not attempt to invoke npm. The client is built separately.

### React Client

- **Dockerfile:** `ArbiScannerAdminPannel/Dockerfile.client`
- **Build context:** `ArbiScannerAdminPannel/` directory
- **Exposed port:** 80
- **Base images:** `node:20-alpine` (build) → `nginx:alpine` (runtime)
- `VITE_API_URL` is accepted as a build argument and baked into the bundle at build time.
- nginx serves the static bundle and proxies `/api/` requests to the API container.

### Using Docker Compose (development/local)

From the `ArbiScannerAdminPannel/` directory:

```bash
docker compose up --build
```

This starts the API (port 8080), the React client (port 3000), PostgreSQL 17, and Redis 7.

For a production deployment from the monorepo root, pass the build context explicitly:

```bash
docker build \
  -f ArbiScannerAdminPannel/Dockerfile \
  -t arbiscanner-admin-api \
  .

docker build \
  -f ArbiScannerAdminPannel/Dockerfile.client \
  --build-arg VITE_API_URL=http://your-server:8081 \
  -t arbiscanner-admin-client \
  ArbiScannerAdminPannel/
```

---

## Database Migrations

Migrations apply only to `AdminPanelAppDbContext` (the `ArbiScannerAdminPanelDb` database). Run them from the API project directory so EF Core can locate the startup project:

```bash
cd ArbiScannerAdminPannel/ArbiScannerAdminPanel.API

# Apply all pending migrations
dotnet ef database update --context AdminPanelAppDbContext

# Create a new migration (when you change domain models)
dotnet ef migrations add <MigrationName> \
  --context AdminPanelAppDbContext \
  --project ../ArbiScannerAdminPanel.Infrastructure \
  --startup-project .
```

The `ArbiScannerBot` database schema is owned by ArbiScannerWebApp. Do not create migrations targeting `AppDbContext` from this project.

---

## Seeding Initial Users

The application seeds two roles (`Administrator` and `Manager`) and a default set of subscription tiers on every startup automatically. These operations are idempotent.

Admin and manager user accounts are created only when `Seed:Enabled` is `true`. This is intentional: credentials are never hard-coded, and seeding is opt-in.

**First deployment steps:**

1. Set `Seed:Enabled` to `true` and provide all four credential fields (`AdminUserName`, `AdminPassword`, `ManagerUserName`, `ManagerPassword`).
2. Start the application. Users are created on startup and assigned the `Administrator` and `Manager` roles respectively.
3. Set `Seed:Enabled` back to `false` (or remove the environment variable) before the next deployment. The seeder will skip creation if the usernames already exist, but disabling it after first use is the recommended practice.

If `Seed:Enabled` is `true` but `AdminUserName` or `AdminPassword` is missing, the application throws `InvalidOperationException` at startup and will not start.

---

## Project Structure

```
ArbiScannerAdminPannel/                  <- submodule root (double-n is intentional at the folder level)
├── ArbiScannerAdminPanel.Domain/        <- domain models, identity entities, value objects
│   └── Models/
│       ├── AdminUserModel.cs
│       ├── AdminRefreshTokenModel.cs
│       ├── SubscriptionModel.cs
│       ├── UserSubscriptionModel.cs
│       ├── PaymentModel.cs
│       ├── UserSubscriptionPayment.cs
│       └── JwtOptions.cs
├── ArbiScannerAdminPanel.Abstractions/  <- interfaces for all services and repositories
│   └── Interfaces/
│       ├── Services/
│       └── Repositories/
├── ArbiScannerAdminPanel.Application/   <- use case implementations, business logic
│   └── Services/
│       ├── AccountService.cs
│       ├── UsersService.cs
│       ├── SubscriptionService.cs
│       ├── PaymentsService.cs
│       └── OxaPayService.cs
├── ArbiScannerAdminPanel.Infrastructure/ <- EF Core, Identity, Redis, repository implementations
│   ├── DbContext/
│   │   └── AdminPanelAppDbContext.cs
│   ├── Migrations/
│   └── Repositories/
├── ArbiScannerAdminPanel.API/           <- ASP.NET Core 10 Web API, composition root
│   ├── Controllers/
│   │   ├── AccountController.cs
│   │   ├── UsersController.cs
│   │   ├── SubscriptionsController.cs
│   │   └── PaymentsController.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Program.cs
│   └── appsettings.json
├── ArbiScannerAdminPanel.Client/        <- React 19 + Vite + TypeScript SPA
│   ├── src/
│   ├── package.json
│   └── vite.config.ts
├── ArbiScannerAdminPanel.sln
├── Dockerfile                           <- API multi-stage build (build context: repo root)
├── Dockerfile.client                    <- Client multi-stage build (Node 20 -> nginx)
├── docker-compose.yml
└── nginx.conf                           <- nginx config for the client container
```
