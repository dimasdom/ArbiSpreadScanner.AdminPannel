using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ArbiScannerAdminPanel.IntegrationTests.Support;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace ArbiScannerAdminPanel.IntegrationTests.Fixtures;

// Spins up a real Keycloak container and imports the actual production realm
// export (keycloak/realm-export/arbiscanner-admin-realm.json, with sslRequired
// patched to "none" and two throwaway service-account clients appended in a
// temp copy - see BuildImportableRealmFile) - this is the one fixture in the
// suite that proves the API's Authority-based JWKS discovery, and the flat
// "role" claim protocol mapper (unique to this realm - arbiscanner-web has no
// roles), really work against a live IdP and that the shipped realm export
// itself imports cleanly. Everything else in the suite uses the fast
// forged-JWT path instead (see JwtTestSettings/JwtTestTokenFactory).
public sealed class KeycloakTestFixture : IAsyncLifetime
{
    private const string RealmName = "arbiscanner-admin";
    private const string AdminUsername = "admin";
    private const string AdminPassword = "admin";

    // Throwaway confidential clients, appended to the imported realm file (not part
    // of the shipped realm export) - one assigned the Administrator role, one
    // Manager, so both halves of the [Authorize(Roles = "Administrator")] boundary
    // get exercised against genuinely Keycloak-issued, Keycloak-signed tokens
    // carrying the real flat "role" claim mapper.
    private const string AdminClientId = "integration-test-admin";
    private const string AdminClientSecret = "integration-test-admin-secret";
    private const string ManagerClientId = "integration-test-manager";
    private const string ManagerClientSecret = "integration-test-manager-secret";

    private readonly string _importedRealmFile = BuildImportableRealmFile();

    private KeycloakContainer? _keycloak;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(Images.Postgres)
        .WithDatabase("ArbiScannerAdminPanelDb")
        .WithUsername("postgres")
        .WithPassword("REDACTED")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder(Images.Redis)
        .Build();

    internal CustomWebApplicationFactory Factory { get; private set; } = default!;
    internal string Authority { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _keycloak = new KeycloakBuilder(Images.Keycloak)
            .WithUsername(AdminUsername)
            .WithPassword(AdminPassword)
            .WithRealm(_importedRealmFile)
            .Build();

        await Task.WhenAll(_keycloak.StartAsync(), _postgres.StartAsync(), _redis.StartAsync());

        Authority = $"{_keycloak.GetBaseAddress().TrimEnd('/')}/realms/{RealmName}";

        Factory = new CustomWebApplicationFactory(
            new Dictionary<string, string?>
            {
                ["Jwt:Authority"] = Authority,
                // The throwaway clients' tokens aren't audience-mapped to
                // arbiscanner-admin-api (that mapper only ships on the production
                // clients) - this fixture exists to prove Authority/JWKS/issuer/
                // signature/role-claim validation against a live IdP, not audience
                // enforcement (which forged-JWT tests already cover cheaply).
                ["Jwt:Audience"] = string.Empty,
                ["ConnectionStrings:AdminConnection"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["Redis:Endpoint"] = _redis.GetConnectionString(),
                ["Observability:Enabled"] = "false",
            },
            configureTestServices: services =>
            {
                // Jwt:Authority (set above) already points AddAuthenticationJwt at this
                // real container, so the framework's own JwtBearerPostConfigureOptions
                // wires up real JWKS discovery/signature validation unmodified. Two
                // things are relaxed relative to production: RequireHttpsMetadata (the
                // raw testcontainer serves plain HTTP) and audience validation (see
                // above). This must run as a Configure (not PostConfigure) - the
                // framework's own JwtBearerPostConfigureOptions throws on a plain-HTTP
                // Authority when RequireHttpsMetadata is still true, and all Configure
                // delegates run before any PostConfigure ones.
                services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                });
            });

        _ = Factory.Services;
    }

    internal Task<string> GetAdministratorTokenAsync() => GetServiceAccountTokenAsync(AdminClientId, AdminClientSecret);

    internal Task<string> GetManagerTokenAsync() => GetServiceAccountTokenAsync(ManagerClientId, ManagerClientSecret);

    private async Task<string> GetServiceAccountTokenAsync(string clientId, string clientSecret)
    {
        using var http = new HttpClient();
        using var response = await http.PostAsync(
            $"{Authority}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
            }));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("access_token").GetString()!;
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        var disposeTasks = new List<Task> { _postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask() };
        if (_keycloak is not null)
        {
            disposeTasks.Add(_keycloak.DisposeAsync().AsTask());
        }
        await Task.WhenAll(disposeTasks);
        File.Delete(_importedRealmFile);
    }

    // Copies the actual production realm export to a temp file with sslRequired
    // patched to "none" (this fixture proves resource-server JWKS/role validation
    // against a live IdP, not Keycloak's own login-transport security policy) and
    // two throwaway service-account clients + their role-assigned service-account
    // users appended, so the import brings up fully-provisioned Administrator- and
    // Manager-scoped identities with no post-import kcadm step needed. Production's
    // shipped file (keycloak/realm-export/arbiscanner-admin-realm.json) is
    // untouched - only this temp copy differs.
    private static string BuildImportableRealmFile()
    {
        var sourcePath = FindRealmExportFile();
        var json = JsonNode.Parse(File.ReadAllText(sourcePath))!.AsObject();
        json["sslRequired"] = "none";

        var clients = json["clients"]!.AsArray();
        clients.Add(BuildServiceAccountClient(AdminClientId, AdminClientSecret));
        clients.Add(BuildServiceAccountClient(ManagerClientId, ManagerClientSecret));

        json["users"] = new JsonArray(
            BuildServiceAccountUser(AdminClientId, "Administrator"),
            BuildServiceAccountUser(ManagerClientId, "Manager"));

        var tempPath = Path.Combine(Path.GetTempPath(), $"arbiscanner-admin-realm-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(tempPath, json.ToJsonString());
        return tempPath;
    }

    private static JsonObject BuildServiceAccountClient(string clientId, string secret) => new()
    {
        ["clientId"] = clientId,
        ["secret"] = secret,
        ["enabled"] = true,
        ["publicClient"] = false,
        ["protocol"] = "openid-connect",
        ["standardFlowEnabled"] = false,
        ["implicitFlowEnabled"] = false,
        ["directAccessGrantsEnabled"] = false,
        ["serviceAccountsEnabled"] = true,
        ["protocolMappers"] = new JsonArray(new JsonObject
        {
            ["name"] = "realm-roles-flat",
            ["protocol"] = "openid-connect",
            ["protocolMapper"] = "oidc-usermodel-realm-role-mapper",
            ["consentRequired"] = false,
            ["config"] = new JsonObject
            {
                ["claim.name"] = "role",
                ["jsonType.label"] = "String",
                ["multivalued"] = "true",
                ["id.token.claim"] = "false",
                ["access.token.claim"] = "true",
                ["userinfo.token.claim"] = "false",
            },
        }),
    };

    private static JsonObject BuildServiceAccountUser(string clientId, string realmRole) => new()
    {
        ["username"] = $"service-account-{clientId}",
        ["enabled"] = true,
        ["serviceAccountClientId"] = clientId,
        ["realmRoles"] = new JsonArray(realmRole),
    };

    private static string FindRealmExportFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "keycloak", "realm-export", "arbiscanner-admin-realm.json")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Could not locate keycloak/realm-export/arbiscanner-admin-realm.json by walking up from the test assembly's output directory.");
        }

        return Path.Combine(dir.FullName, "keycloak", "realm-export", "arbiscanner-admin-realm.json");
    }
}
