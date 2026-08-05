using System.Net.Http.Headers;

namespace ArbiScannerAdminPanel.IntegrationTests.Support;

// Replaces the old Authenticate->cookie login dance (Keycloak owns that flow now,
// not this API) with a forged Bearer token carrying whatever realm roles the
// test needs.
internal static class AuthenticatedClientFactory
{
    public static HttpClient CreateAuthenticatedClient(this CustomWebApplicationFactory factory, string? sub = null, params string[] roles)
    {
        sub ??= Guid.NewGuid().ToString();

        var client = factory.CreateClient();
        var token = JwtTestTokenFactory.CreateToken(sub, roles);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
