using ArbiScannerAdminPanel.IntegrationTests.Support;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace ArbiScannerAdminPanel.IntegrationTests.Fixtures;

public sealed class AdminApiTestFixture : IAsyncLifetime
{
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

        // AppDbContext (the "DefaultConnection") isn't exercised by the role-authorization
        // flow this fixture backs - point it at the same Postgres container rather than
        // paying for a second one just to satisfy DI registration.
        Factory = new CustomWebApplicationFactory(
            new Dictionary<string, string?>(JwtTestSettings.ConfigOverrides)
            {
                ["ConnectionStrings:AdminConnection"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["Redis:Endpoint"] = _redis.GetConnectionString(),
                ["Observability:Enabled"] = "false",
            },
            configureTestServices: JwtTestSettings.ConfigureTestJwtBearer);

        _ = Factory.Services;
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
