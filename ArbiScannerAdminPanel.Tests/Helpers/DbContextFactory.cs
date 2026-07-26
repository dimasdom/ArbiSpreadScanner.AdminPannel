using ArbiScannerAdminPanel.Infrastructure.DbContext;
using ArbiScannerWeb.Infrastructure.DbContext;
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
}
