# ArbiScannerAdminPannel — Architecture Review

## Summary

The project has a clear layered intent (Core → Domain/Application → Infrastructure → Server) but contains critical security vulnerabilities, cross-project coupling, layer violations, and production-readiness gaps that need to be resolved.

---

## Issues

### 1. Hardcoded JWT Secret and Test Credentials in Source Code (Critical — Security)

**Files affected:** `ArbiScannerAdminPannel.Domain/StartupSetup.cs`, `ArbiScannerAdminPannel.Domain/Services/AccountService.cs`

JWT issuer, audience, and signing key are hardcoded as plain strings with obvious placeholder values (`"TESTISSUER"`, `"TESTAUDIENCE"`, `"TESTTESTESTESTTESTTESTESTESTTESTTESTTESTESTEST"`). The same literal key string appears in both startup config and the token generation service — any change must be made in two places.

```csharp
IssuerSigningKey = new SymmetricSecurityKey(
    Encoding.ASCII.GetBytes("TESTTESTESTESTTESTTESTESTESTTESTTESTTESTESTEST"))
```

**Fix:**
- Move issuer, audience, and signing key to `appsettings.json` (or secrets/environment variables for production).
- Read them via `IOptions<JwtOptions>` (same pattern already used in `ArbiScannerWebApp`).
- Never duplicate the key string — derive it from a single config source in both validation and generation.

---

### 2. Default Admin Credentials Seeded in Plain Text (Critical — Security)

**File:** `ArbiScannerAdminPannel.API/Program.cs`

```csharp
var adminUser = new AdminUserModel { UserName = "admin" };
await userManager.CreateAsync(adminUser, "Admin@123");
```

Fixed username `admin` and password `Admin@123` are hardcoded in startup. These get seeded into any environment (including production) on first boot.

**Fix:**
- Read seed credentials from environment variables or a secrets manager (`IConfiguration["Seed:AdminPassword"]`).
- Never commit default credentials to source control.
- Force a password change on first login, or make seeding opt-in via an environment flag.

---

### 3. Admin Panel Directly Accesses the Web App's Database (Critical)

**Files affected:** `ArbiScannerAdminPannel.Domain/Services/UsersService.cs`, `ArbiScannerAdminPannel.Domain/Services/PaymentsService.cs`, `ArbiScannerAdminPannel.Domain/Services/SubscriptionService.cs`, `ArbiScannerAdminPannel.Domain/StartupSetup.cs`

The admin panel registers and directly queries `AppDbContext` — the EF context belonging to `ArbiScannerWebApp`. This couples two independent services at the database level:

```csharp
using ArbiScannerWeb.Infrastructure.DbContext; // in admin panel services
private readonly AppDbContext _appDbContext;     // injected into admin services
```

This means:
- Schema changes in the web app can silently break the admin panel.
- The admin panel must reference the web app's infrastructure assembly.
- Migrations in one project can destroy the other's data.

**Fix:** The admin panel should expose an internal API or message-based interface to read user data from the web app. Alternatively, define a read-model projection of needed user data in a shared schema that both services own by contract — not by sharing a DbContext assembly.

---

### 4. Services in the Domain Layer Directly Access DbContext (High)

**Files affected:** `UsersService.cs`, `PaymentsService.cs`, `SubscriptionService.cs`

All domain-layer services directly inject and query `AdminPanelAppDbContext` and `AppDbContext`. There is no repository abstraction — the domain layer knows about EF Core internals.

**Fix:** Introduce repository interfaces in `ArbiScannerAdminPannel.Core` (e.g. `IPaymentRepository`, `ISubscriptionRepository`) with implementations in `ArbiScannerAdminPannel.Infrastructure`. Domain services should depend on repository abstractions, not on `DbContext` directly. This is especially important given that `UsersService` mixes synchronous and asynchronous DbContext calls (see issue #6).

---

### 5. `AccountController` Has No Authorization (High — Security)

**File:** `ArbiScannerAdminPannel.API/Controllers/AccountController.cs`

The `Authenticate` endpoint is correctly public, but the controller class has no `[ApiController]` model validation and no rate limiting. More critically, it logs the attempted username to `Console.WriteLine` on every request — this is a data exposure risk and should use structured logging via `ILogger`.

```csharp
Console.WriteLine($"Received authentication request for user: {accountAuthenticateDTO.UserName}");
```

**Fix:**
- Replace `Console.WriteLine` with `ILogger<AccountController>`.
- Consider adding brute-force protection (rate limiting, lockout) on the authenticate endpoint.

---

### 6. Mixed Sync/Async DbContext Calls (High)

**File:** `ArbiScannerAdminPannel.Domain/Services/UsersService.cs`

`DeleteClientUser` calls `.SaveChanges()` (synchronous) inside an `async Task` method. Calling synchronous EF methods from async code can cause thread-pool starvation under load.

```csharp
public async Task<Result> DeleteClientUser(string id)
{
    ...
    _appDbContext.SaveChanges(); // should be await SaveChangesAsync()
}
```

**Fix:** Replace all `.SaveChanges()` / `.FirstOrDefault()` / `.ToList()` calls inside `async` methods with their `Async` counterparts.

---

### 7. `EnsureCreatedAsync` Used Instead of Migrations (High)

**File:** `ArbiScannerAdminPannel.API/Program.cs`

```csharp
await dbContext.Database.EnsureCreatedAsync();
```

The project has a Migrations folder with real migrations. `EnsureCreatedAsync` bypasses migrations entirely — new migrations will never be applied after initial creation. This is the same issue identified in `ArbiScannerWebApp`.

**Fix:** Replace with `await dbContext.Database.MigrateAsync()`.

---

### 8. Domain Entity (`UserSubscriptionPayment`) Returned Directly from API (High)

**Files affected:** `Controllers/PaymentsController.cs`

Multiple endpoints return `ActionResult<Result<UserSubscriptionPayment>>` — the raw EF entity including navigation properties (`Subscription`, `Payment`). This leaks schema details, navigation cycles can cause JSON serialization errors, and the response shape is unversioned.

**Fix:** Map `UserSubscriptionPayment` to a `UserSubscriptionPaymentDTO` before returning it from controllers. DTOs already exist in the project for other entities — apply the same pattern here.

---

### 9. Layer Naming Inconsistency — "Domain" Layer Contains Application Logic (Medium)

The folder/project is named `ArbiScannerAdminPannel.Domain` but its `.csproj` is `ArbiScannerAdminPannel.Application.csproj` and its namespace is `ArbiScannerAdminPannel.Application`. The layer contains services (application logic), not domain entities/rules. The entities live in `Core`.

This is the inverse of standard Clean Architecture naming: the "Domain" folder has application services, while the "Core" folder has domain models and interfaces.

**Fix:** Rename the project and folder to `ArbiScannerAdminPannel.Application` to match the `.csproj`. Entities and interfaces stay in `Core`. This aligns the naming with the rest of the solution.

---

### 10. `ccxt` Exchange Library Imported in a Subscription Service (Medium)

**File:** `ArbiScannerAdminPannel.Domain/Services/SubscriptionService.cs`

```csharp
using ccxt;
```

The `ccxt` cryptocurrency exchange library is imported in `SubscriptionService` but appears unused there. This is either a stale import or a sign that exchange logic has leaked into the wrong layer.

**Fix:** Remove the unused `using ccxt;` statement. If exchange rate lookups are genuinely needed during subscription logic, extract them into a dedicated service (e.g. `IExchangeRateService`) and inject it.

---

### 11. `GetPaymentsForUser` Accepts `userId` from Query String Without Authorization Check (Medium — Security)

**File:** `ArbiScannerAdminPannel.API/Controllers/PaymentsController.cs`

```csharp
[HttpGet("GetPaymentsForUser")]
public async Task<ActionResult<Result<List<UserSubscriptionPayment>>>> GetPaymentsForUser([FromQuery] string userId)
```

Any authenticated admin user can query payment data for any `userId` by passing an arbitrary ID in the query string. There is no ownership check or role restriction.

**Fix:** Either restrict this endpoint to `Administrator` role only, or validate that the caller has permission to view the requested user's data.

---

### 12. `CreatePaymentForUser` Accepts a Full Entity from Request Body (Medium — Security)

**File:** `ArbiScannerAdminPannel.API/Controllers/PaymentsController.cs`

```csharp
[HttpPost("CreatePaymentForUser")]
public async Task<ActionResult<Result<UserSubscriptionPayment>>> CreatePaymentForUser(
    [FromBody] UserSubscriptionPayment payment)
```

The endpoint accepts a raw `UserSubscriptionPayment` entity (including `Id`, `PaymentId`, `SubscriptionId`) from the request body. A caller can supply arbitrary IDs to forge or overwrite payment records.

**Fix:** Accept a dedicated `CreatePaymentRequestDTO` that only contains the fields a caller is allowed to set (e.g. `UserId`, `SubscriptionId`). Server-side code should assign all other fields.

---

### 13. `RequireHttpsMetadata = false` in JWT Options (Medium — Security)

**File:** `ArbiScannerAdminPannel.Domain/StartupSetup.cs`

```csharp
options.RequireHttpsMetadata = false;
```

This disables HTTPS requirement for the JWT Bearer metadata endpoint. Combined with the hardcoded test key, this means tokens could be transmitted and validated over plain HTTP.

**Fix:** Set `RequireHttpsMetadata = true` for production. Use an environment check if local development requires HTTP, e.g. `options.RequireHttpsMetadata = !builder.Environment.IsDevelopment()`.

---

### 14. Typo in Namespace — "Infrastructure" (Low)

**Affects:** `ArbiScannerAdminPannel.Infrastructure` project and all its namespaces.

Same typo as in `ArbiScannerWebApp` — "Infrastructure" instead of "Infrastructure". This propagates through `using` statements across the entire solution.

**Fix:** Rename the project folder and update all namespace references to `ArbiScannerAdminPannel.Infrastructure`.

---

### 15. `.csproj.Backup.tmp` Committed to Source Control (Low)

**File:** `ArbiScannerAdminPannel.API/ArbiScannerAdminPannel.API.csproj.Backup.tmp`

A Visual Studio backup file is tracked in the repository.

**Fix:** Add `*.Backup.tmp` to `.gitignore` and remove this file from the repository.

---

### 16. No Exception Handling Middleware (Low)

`ArbiScannerWebApp` has `ExceptionHandlingMiddleware`. The admin panel has no equivalent — unhandled exceptions will return stack traces to clients in development and potentially in production.

**Fix:** Add a global exception handling middleware (or use `app.UseExceptionHandler`) to return consistent error responses and prevent leaking internal details.

---

## Priority Summary

| # | Issue | Priority |
|---|-------|----------|
| 1 | Hardcoded JWT secret and test values | Critical / Security |
| 2 | Default admin credentials in plain code | Critical / Security |
| 3 | Admin panel queries web app's DbContext directly | Critical |
| 4 | Domain services bypass repository abstraction | High |
| 5 | `Console.WriteLine` logging auth attempts | High / Security |
| 6 | Sync DbContext calls inside async methods | High |
| 7 | `EnsureCreatedAsync` conflicts with migrations | High |
| 8 | Domain entity returned directly from API | High |
| 11 | `GetPaymentsForUser` no ownership/role check | Medium / Security |
| 12 | `CreatePaymentForUser` accepts raw entity body | Medium / Security |
| 13 | `RequireHttpsMetadata = false` | Medium / Security |
| 9 | Domain/Application project naming mismatch | Medium |
| 10 | Unused `ccxt` import in SubscriptionService | Medium |
| 16 | No global exception handling middleware | Low |
| 14 | "Infrastructure" typo in namespace | Low |
| 15 | `.csproj.Backup.tmp` in source control | Low |
