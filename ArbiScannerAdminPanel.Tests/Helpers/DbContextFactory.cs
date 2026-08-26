using ArbiScannerAdminPanel.Infrastructure.DbContext;
using ArbiScannerWeb.Infrastructure.DbContext;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ArbiScannerAdminPanel.Tests.Helpers;

internal static class DbContextFactory
{
    internal static AdminPanelAppDbContext CreateAdminPanelDbContext()
    {
        var options = new DbContextOptionsBuilder<AdminPanelAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AdminPanelAppDbContext(options);
    }

    internal static AppDbContext CreateAppDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // A relational (SQLite, in-memory) provider is needed to exercise repository code paths that
    // require Database.BeginTransactionAsync() - the InMemory provider above doesn't support transactions.
    internal static (AdminPanelAppDbContext Context, SqliteConnection Connection) CreateAdminPanelSqliteDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AdminPanelAppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AdminPanelAppDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    internal static (AppDbContext Context, SqliteConnection Connection) CreateAppSqliteDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }
}
