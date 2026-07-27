using ArbiScannerAdminPanel.IntegrationTests.Support;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace ArbiScannerAdminPanel.IntegrationTests.Fixtures;

public sealed class AdminApiTestFixture : IAsyncLifetime
{
    public const string AdminUserName = "integration-admin";
    public const string AdminPassword = "IntegrationTest@123";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(Images.Postgres)
        .WithDatabase("ArbiScannerAdminPanelDb")
        .WithUsername("postgres")
        .WithPassword("REDACTED")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder(Images.Redis)
        .Build();

    internal CustomWebApplicationFactory Factory { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        // AppDbContext (the "DefaultConnection") isn't exercised by the account/auth flow this
        // fixture backs - point it at the same Postgres container rather than paying for a
        // second one just to satisfy DI registration.
        Factory = new CustomWebApplicationFactory(
            new Dictionary<string, string?>(JwtTestSettings.ConfigOverrides)
            {
                ["ConnectionStrings:AdminConnection"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["Redis:Endpoint"] = _redis.GetConnectionString(),
                ["Observability:Enabled"] = "false",
                ["Seed:Enabled"] = "true",
                ["Seed:AdminUserName"] = AdminUserName,
                ["Seed:AdminPassword"] = AdminPassword,
            });

        _ = Factory.Services;
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
