# EF Core Migration Scripts — ArbiScannerAdminPannel

## Create a new migration

```bash
dotnet ef migrations add <MigrationName> \
  --project ArbiScannerAdminPannel.Infrastructure \
  --startup-project ArbiScannerAdminPannel.API \
  --context AdminPanelAppDbContext
```

**Example — initial schema:**
```bash
dotnet ef migrations add InitialCreate \
  --project ArbiScannerAdminPannel.Infrastructure \
  --startup-project ArbiScannerAdminPannel.API \
  --context AdminPanelAppDbContext
```

## Apply migrations to the database

```bash
dotnet ef database update \
  --project ArbiScannerAdminPannel.Infrastructure \
  --startup-project ArbiScannerAdminPannel.API \
  --context AdminPanelAppDbContext
```

## Roll back to a specific migration

```bash
dotnet ef database update <MigrationName> \
  --project ArbiScannerAdminPannel.Infrastructure \
  --startup-project ArbiScannerAdminPannel.API \
  --context AdminPanelAppDbContext
```

## Remove the last unapplied migration

```bash
dotnet ef migrations remove \
  --project ArbiScannerAdminPannel.Infrastructure \
  --startup-project ArbiScannerAdminPannel.API \
  --context AdminPanelAppDbContext
```

## List all migrations and their applied status

```bash
dotnet ef migrations list \
  --project ArbiScannerAdminPannel.Infrastructure \
  --startup-project ArbiScannerAdminPannel.API \
  --context AdminPanelAppDbContext
```

## Notes

- Run all commands from the solution root: `/Users/dmytrobartash/Projects/ArbiScannerAdminPannel`
- Migration files are generated inside `ArbiScannerAdminPannel.Infrastructure/Migrations/`
- The startup project is `ArbiScannerAdminPannel.API` (provides the connection string via `appsettings.json` key `AdminConnection`)
- The infrastructure project is `ArbiScannerAdminPannel.Infrastructure` (contains `AdminPanelAppDbContext`)
- `--context AdminPanelAppDbContext` is required — the solution contains multiple DbContexts
