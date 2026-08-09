# ArbiScannerAdminPanel
[![Quality gate](https://sonarcloud.io/api/project_badges/quality_gate?project=dimasdom_ArbiSpreadScanner.AdminPannel)](https://sonarcloud.io/summary/new_code?id=dimasdom_ArbiSpreadScanner.AdminPannel)

Admin and manager panel for the ArbiScanner platform. Provides administrators and managers with tools to view and manage users, handle subscriptions, process crypto payments via OxaPay, configure system settings, and monitor platform activity. This is a separate application from the user-facing ArbiScannerWebApp.


---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Technologies](#technologies)
- [Two-Database Setup](#two-database-setup)
- [OxaPay Webhook Verification](#oxapay-webhook-verification)
- [Prerequisites](#prerequisites)
- [Running Locally](#running-locally)
- [Configuration](#configuration)
- [Environment Variables (Docker)](#environment-variables-docker)
- [Docker Build](#docker-build)
- [Database Migrations](#database-migrations)
- [Seeding Initial Users](#seeding-initial-users)
- [CI/CD](#cicd)
- [Testing](#testing)
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

**Note on error handling:** this API does not maintain its own exception middleware or result-serialization code. It already references `ArbiScannerWeb.Infrastructure` (see [Two-Database Setup](#two-database-setup) for why) and reuses that project's `ExceptionHandlingMiddleware`, `ResultStatusCodeFilter`, and `TypedErrors`/`ErrorCodes` helpers, so both APIs return the identical JSON error envelope (`{ isSuccess, isFailed, errorCode, message, value, reasons }`). Controllers call `Result.Fail(TypedErrors.Validation("..."))`/`.ToSerializable()` the same way ArbiScannerWeb's do — see that project's README for the full write-up of the contract.

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
- `OxaPayService` — wraps the OxaPay HTTP API for invoice creation and status checks, and verifies the HMAC-SHA512 signature on incoming payment webhooks — see [OxaPay Webhook Verification](#oxapay-webhook-verification)

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
- `PaymentsController` — payment records, plus `POST api/payments/webhook`: the only anonymous endpoint in this controller (every other action requires `[Authorize]`), verified by OxaPay's HMAC signature instead — see [OxaPay Webhook Verification](#oxapay-webhook-verification)

Additional features:
- JWT Bearer authentication (access token: 15 min, refresh token: 7 days)
- CORS policy controlled by configuration
- OpenAPI (Scalar) — available in development at `/openapi`
- Serilog request logging, enriched with machine name and thread ID, shipped to Grafana Loki
- Global exception handling via `ArbiScannerWeb.Infrastructure`'s shared `ExceptionHandlingMiddleware` and `ResultStatusCodeFilter` (see the error handling note above)
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
| Logging | Serilog → Grafana Loki (enriched with `TraceId`/`SpanId` via `Serilog.Enrichers.Span`) |
| Tracing | OpenTelemetry → Grafana Tempo (ASP.NET Core, HTTP client, EF Core, Redis instrumentation) |
| Metrics | OpenTelemetry Prometheus exporter at `/metrics` |
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

## OxaPay Webhook Verification

Payment status used to be confirmed purely by polling (`GetActivePaymentForUser` → `OxaPayService.GetInvoiceStatus`) — there was no webhook receiver at all. `POST api/payments/webhook` is a real one:

- **Signature:** OxaPay signs the raw POST body with HMAC-SHA512, keyed by the merchant API key, sent as the `HMAC` header. `OxaPayService.VerifyWebhookSignature` recomputes it and compares using `CryptographicOperations.FixedTimeEquals` (constant-time — OxaPay's own sample code uses a plain `==`, which is a timing-attack gap this doesn't have).
- **Why the controller reads `Request.Body` directly:** the signature covers the *exact raw bytes* OxaPay sent. Using `[FromBody]` model binding would parse then re-serialize the JSON — different whitespace or property order would silently break the signature check before verification even ran. `PaymentsController.Webhook()` reads and buffers the raw body itself instead.
- **Config:** `OxaPay:*` is bound to a typed `OxaPaySettings` via `IOptions<T>` (see [Configuration](#configuration)), matching every other secret in this codebase's pattern — it used to be read via raw `IConfiguration` string indexing.
- **Replay handling:** the payload has no nonce, only a Unix timestamp (`date`), so events older than 1 hour are rejected as stale (`PaymentsService.HandleOxaPayWebhookAsync`) — defense-in-depth, not true replay protection. The actual idempotency guarantee is `PaymentsService.AcceptPayment`'s existing `Status == Completed` short-circuit: a redelivered webhook for an already-completed payment is a no-op, not a re-assignment of the subscription.
- **Polling is still there** as a fallback/reconciliation path — this didn't replace it.

**Getting the callback URL right matters more than it sounds.** This API is reachable at whatever `ADMIN_API_URL` in your `.env` currently points to (typically a bare `host:8081`, not the public-facing `arbiscannerwebapp.site` domain — that domain's nginx routes `/api/` to the *WebApp* API, which has no payments controller at all). Registering the wrong domain with OxaPay means the webhook silently never arrives, with polling masking that it isn't working.

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
- `Jwt:Authority` — the Keycloak realm issuer, e.g. `http://localhost:8082/realms/arbiscanner-admin` for local dev (see monorepo root's `keycloak/README.md`)
- `Jwt:Audience` — `arbiscanner-admin-api`
- `Redis:Endpoint` — e.g. `localhost:6379`

### 3. Apply database migrations

```bash
cd ArbiScannerAdminPannel/ArbiScannerAdminPanel.API

dotnet ef database update --context AdminPanelAppDbContext
```

The shared `ArbiScannerBot` database is managed by ArbiScannerWebApp. Run its migrations separately if the database does not already exist.

### 4. Provision the initial staff accounts

Auth is Keycloak-backed (`arbiscanner-admin` realm, staff-only, no self-registration), so `Administrator`/`Manager` accounts are created in Keycloak, not seeded by this API. From the monorepo root, with `ADMIN_USERNAME`/`ADMIN_PASSWORD`/`MANAGER_USERNAME`/`MANAGER_PASSWORD` set in `.env`:

```bash
set -a && source .env && set +a && ./keycloak/configure-admin-users.sh
```

Both accounts get a temporary password, forcing a reset via Keycloak's `UPDATE_PASSWORD` required action on first login. See the monorepo root's `keycloak/README.md` for the full one-time setup.

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
    "Authority": "https://auth.arbiscannerwebapp.site/realms/arbiscanner-admin",
    "Audience": "arbiscanner-admin-api"
  },
  "Cors": {
    "AllowedOrigins": "http://localhost:5174"
  },
  "Redis": {
    "Endpoint": "localhost:6379"
  },
  "OxaPay": {
    "BaseUrl": "https://api.oxapay.com/v1",
    "MerchantApiKey": "...",
    "DefaultCurrency": "USD",
    "DefaultLifetime": 30,
    "Sandbox": true
  },
  "OpenTelemetry": {
    "Endpoint": "http://localhost:4317"
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
| `OIDC_AUTHORITY_ADMINPANEL` | `Jwt:Authority` | Keycloak realm issuer (`arbiscanner-admin` realm) `AddJwtBearer` validates tokens against |
| `OIDC_AUDIENCE_ADMINPANEL` | `Jwt:Audience` | Expected audience claim — `arbiscanner-admin-api` |
| `ADMIN_CLIENT_URL` | `Cors:AllowedOrigins` | CORS allowed origin for the React client |
| `Redis__Endpoint` | `Redis:Endpoint` | Redis connection string, e.g. `redis:6379` |
| `ADMIN_USERNAME` | — | Initial Administrator account username, consumed by `keycloak/configure-admin-users.sh`, not by this API directly |
| `ADMIN_PASSWORD` | — | Initial Administrator account password (temporary, forced reset on first login) |
| `MANAGER_USERNAME` | — | Initial Manager account username |
| `MANAGER_PASSWORD` | — | Initial Manager account password (temporary, forced reset on first login) |
| `OXAPAY_BASE_URL` | `OxaPay:BaseUrl` | OxaPay API base URL |
| `OXAPAY_MERCHANT_API_KEY` | `OxaPay:MerchantApiKey` | OxaPay merchant key |
| `OXAPAY_DEFAULT_CURRENCY` | `OxaPay:DefaultCurrency` | Default invoice currency |
| `OXAPAY_DEFAULT_LIFETIME` | `OxaPay:DefaultLifetime` | Invoice lifetime in minutes |
| `OXAPAY_SANDBOX` | `OxaPay:Sandbox` | `true` for sandbox mode, `false` for production |
| `OpenTelemetry__Endpoint` | `OpenTelemetry:Endpoint` | OTLP gRPC endpoint for Grafana Tempo (e.g. `http://tempo:4317`). Defaults to `http://localhost:4317` from `appsettings.json`. |

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

**Notable migrations:**

| Migration | What it does |
|---|---|
| `AddUserPaymentSubscriptionIndexes` | Adds indexes on `UserSubscriptions` (`UserId`, `EndDate` composite), `UserSubscriptionPayments` (`UserId`), and `Payments` (`TransactionId`, `UserId`) to speed up the lookups `UsersService`/`PaymentsService` do when rendering a user's subscription and payment history. |

A `skills/create-migration.md` note documents the EF Core migration workflow above for reuse across sessions; `skills/create-docker-compose.md` similarly documents the [Docker Build](#docker-build) compose setup.

---

## Seeding

`Program.cs`'s `SeedDatabaseAsync` seeds a default set of subscription tiers (`Basic`/`Standard`/`Premium`) into `AdminPanelAppDbContext` on every startup, unconditionally — idempotent, skipped once any `Subscriptions` row exists.

`Administrator`/`Manager` accounts are **not** seeded by this application — they're Keycloak realm accounts, provisioned once via `keycloak/configure-admin-users.sh` (see [Provision the initial staff accounts](#4-provision-the-initial-staff-accounts) above and the monorepo root's `keycloak/README.md`).

---

## CI/CD

`.editorconfig` and `Directory.Build.props` enable `AnalysisLevel=latest`/`AnalysisMode=Recommended` with `TreatWarningsAsErrors`. `Directory.Build.props` documents the specific pre-existing warning rule IDs grandfathered in — nullable-safety warnings are not among them and fail the build if introduced.

This repo has its own GitHub Actions, independent of the monorepo root's Actions tab (it's a separate git remote — see the monorepo root's CI/CD section for how the two relate). Three workflows live under `.github/workflows/`:

### `ci.yml` — build, test, quality gate

Runs on every push/PR to `main`:

1. Checks out this repo into `ArbiScannerAdminPannel/` and the sibling `ArbiScannerWebApp` repo into `ArbiScannerWebApp/` in the same job workspace — this `.sln` references `ArbiScannerWeb.Abstractions`/`.Infrastructure` from it directly (see [Two-Database Setup](#two-database-setup) and [Error handling note](#architecture)), so it must be present for both `dotnet build` and SonarCloud's exclusion paths to resolve.
2. A SonarCloud scan (project `dimasdom_ArbiSpreadScanner.AdminPannel`) wraps everything below, with `sonar.projectBaseDir` pinned to the `ArbiScannerAdminPannel/` checkout and `**/ArbiScannerWebApp/**` excluded (that sibling code is scanned by its own repo's CI, not double-counted here); `sonar.qualitygate.wait=true` fails the job on a red quality gate.
3. CodeQL initializes twice — C# (`build-mode: manual`) and JavaScript/TypeScript (`build-mode: none`) — both scoped to `source-root: ArbiScannerAdminPannel` so the sibling checkout isn't scanned twice.
4. `npm ci --ignore-scripts` + `npm test -- --coverage --reporter=junit` in `ArbiScannerAdminPanel.Client`, then the vitest lcov `SF:` paths are rewritten to absolute so SonarCloud's coverage import resolves them.
5. `dotnet restore`/`build` on `ArbiScannerAdminPanel.sln` with analyzers, then `ArbiScannerAdminPanel.Tests` (unit, no Docker required) and `ArbiScannerAdminPanel.IntegrationTests`, both with coverage collection feeding the SonarCloud scan.
6. `.trx` and JUnit results are published as check-run summaries via `dorny/test-reporter`.

Both SonarCloud and CodeQL are free for this public repo; SonarCloud additionally requires a `SONAR_TOKEN` secret.

### `deploy.yml` — manual deploy to the VPS

A `workflow_dispatch`-triggered workflow (optional `dry_run` boolean input) that calls the monorepo root's reusable `deploy-service.yml` (`dimasdom/SpreadScanner/.github/workflows/deploy-service.yml`, pinned to a specific commit SHA) with this repo's specifics: solution/test project paths, `has_client_tests: true` + `client_dir`, the SonarCloud exclusion list, `sibling_repos` set to check out `ArbiScannerWebApp` (needed by both the `dotnet build` and the API's Docker build — see [Cross-submodule references](../README.md#cross-submodule-references-important-non-obvious) in the monorepo root README), and two image specs — `arbiscanner-admin-api` (build context `.`, repo root) and `arbiscanner-admin-client` (build context `ArbiScannerAdminPannel/`, with `VITE_API_URL=/adminapi` baked in at build time).

End to end: tests + client tests + quality gate → build and push `ghcr.io/dimasdom/arbiscanner-admin-api(-client):latest` / `:sha-<commit>` to GHCR → (unless `dry_run: true`) SSH into the VPS and restart `admin-api` then `admin-client` via `scripts/deploy-remote.sh`. Requires `SONAR_TOKEN` plus `VPS_HOST`/`VPS_USER`/`VPS_SSH_KEY`/`VPS_SSH_PORT`/`VPS_DEPLOY_PATH` secrets on this repo.

The root monorepo also has `.github/workflows/docker-build.yml`, since this API's Dockerfile needs repo-root build context — it builds this service's images alongside the other three on every push/PR to `master`, as a build-breakage smoke check separate from this repo's own CI.

### `load-test.yml` — scheduled + on-demand load test

Runs `ArbiScannerAdminPanel.LoadTests` separately (`workflow_dispatch` with `queries_per_minute`/`duration_seconds` inputs, plus a nightly `0 3 * * *` cron at the defaults), gated behind a `load-test` GitHub Environment holding the `ADMINPANEL_LOADTEST_BASE_URL`/`_USERNAME`/`_PASSWORD` secrets — see [ArbiScannerAdminPanel.LoadTests](#arbiscanneradminpanelloadtests) below.

**Health checks:** `/health` covers both Postgres databases this service touches (its own `AdminPanelAppDbContext` and the shared, read-only `AppDbContext`) plus Redis, via `Microsoft.Extensions.Diagnostics.HealthChecks` and the shared check classes in `ArbiScannerWeb.Infrastructure/HealthChecks/` (pulled in via the existing project reference).

---

## Testing

`ArbiScannerAdminPanel.Tests` (xUnit + FluentAssertions + Moq) — 94 tests, no Docker/Testcontainers required.

| File | Coverage |
|---|---|
| `Services/AccountServiceTests` | Login, refresh-token rotation, and reuse-detection (a presented, already-rotated token revokes the whole token chain) |
| `Services/SubscriptionServiceTests` | Subscription CRUD and the Redis-cached lookup path (including TTL clamping) |
| `Services/PaymentsServiceTests` | `AcceptPayment`'s idempotency short-circuit, the polling-triggered accept flow, and `HandleOxaPayWebhookAsync` — paid/non-paid status, invoice/non-invoice type, and stale-event filtering |
| `Services/OxaPayServiceTests` | Invoice generation and status mapping against a faked `HttpClient`, plus `VerifyWebhookSignature`: a matching HMAC (computed independently in the test, not hardcoded) passes, a wrong key or a tampered body fails, and a missing header/key is rejected |

```bash
dotnet test ArbiScannerAdminPanel.Tests/ArbiScannerAdminPanel.Tests.csproj
```

### ArbiScannerAdminPanel.LoadTests

A separate, not-part-of-the-normal-test-run project mirroring `ArbiScannerWebApp`'s `ArbiScannerWeb.LoadTests` (small hand-rolled `SemaphoreSlim`-throttled `LoadRunner`, `Xunit.SkippableFact` so it's skipped by default):

- `LoadTests/SubscriptionsFetchLoadTest` — GET `/api/Subscriptions/GetAllSubscriptions`
- `LoadTests/SubscriptionUpdateLoadTest` — fetches the first subscription and POSTs it back unchanged to `/api/Subscriptions/UpdateSubscription` (idempotent); requires the authenticated account to hold the `Administrator` role, and skips with a clear message if it doesn't

Auth is `POST /api/Account/Authenticate` (`AdminAccountAuthenticateDTO{UserName,Password}`) — despite the API returning the JWT in the response body, it blanks that field and instead sets it as an HttpOnly cookie (`adminpanel.access_token`), so the load test client just needs cookie-container `HttpClient`, same pattern as the WebApp's version.

| Variable | Purpose |
|---|---|
| `ADMINPANEL_LOADTEST_BASE_URL` | Target instance (no trailing slash/path) |
| `ADMINPANEL_LOADTEST_USERNAME` / `ADMINPANEL_LOADTEST_PASSWORD` | A seeded admin account (see [Seeding Initial Users](#seeding-initial-users)) |
| `ADMINPANEL_LOADTEST_QUERIES_PER_MINUTE` | Target rate per endpoint (default `60`) |
| `ADMINPANEL_LOADTEST_DURATION_SECONDS` | Sustained duration (default `60`) |

`LoadRunner` paces strictly off queries-per-minute (one request every `60 / QueriesPerMinute` seconds), and `xunit.runner.json` disables collection parallelism so the two tests run sequentially instead of racing on the same login session.

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
│       ├── JwtOptions.cs
│       ├── OxaPaySettings.cs           <- typed OxaPay:* config (see OxaPay Webhook Verification)
│       └── DTOs/
│           └── OxaPayWebhookPayloadDTO.cs
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
│   ├── Program.cs                      <- registers ArbiScannerWeb.Infrastructure's shared
│   │                                      ExceptionHandlingMiddleware + ResultStatusCodeFilter
│   ├── appsettings.json
│   └── CHANGELOG.md                    <- Visual Studio project-scaffolding log
├── ArbiScannerAdminPanel.Client/        <- React 19 + Vite + TypeScript SPA
│   ├── src/
│   │   ├── components/
│   │   │   └── ErrorState.tsx          <- renders the shared API error envelope
│   │   ├── types/
│   │   │   └── ApiError.ts
│   │   ├── utils/
│   │   │   └── normalizeApiError.ts    <- mirrors ArbiScannerWeb.Client's version (independent npm project, code duplicated not shared)
│   │   └── ...
│   ├── package.json
│   ├── vite.config.ts
│   └── CHANGELOG.md
├── ArbiScannerAdminPanel.sln
├── ARCHITECTURE_REVIEW.md               <- prior architecture review notes
├── skills/                              <- reusable workflow notes (migrations, docker-compose)
├── Dockerfile                           <- API multi-stage build (build context: repo root)
├── Dockerfile.client                    <- Client multi-stage build (Node 20 -> nginx)
├── docker-compose.yml
└── nginx.conf                           <- nginx config for the client container
```
