using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ArbiScannerAdminPanel.LoadTests.Settings;

namespace ArbiScannerAdminPanel.LoadTests.Support;

internal sealed record AuthenticatedSession(HttpClient Client, IReadOnlyList<string> Roles);

// Login is Keycloak's job now, not this API's — obtains a token via the
// Resource Owner Password Credentials grant against the dedicated
// arbiscanner-admin-loadtest client (direct-grant enabled, unlike the
// browser-facing SPA client, which stays PKCE-only). See
// keycloak/realm-export/arbiscanner-admin-realm.json.
internal static class AuthenticatedClientFactory
{
    public static async Task<AuthenticatedSession> CreateAsync(LoadTestSettings settings)
    {
        var accessToken = await RequestAccessTokenAsync(settings);
        var roles = ReadRoleClaims(accessToken);

        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = LoadRunner.MaxConcurrency(settings.QueriesPerMinute)
        };

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(settings.BaseUrl)
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var probeResponse = await client.GetAsync("api/Subscriptions/GetAllSubscriptions?page=1");
        if (!probeResponse.IsSuccessStatusCode)
        {
            client.Dispose();
            throw new InvalidOperationException(
                $"Load test login probe failed for '{settings.Username}' at '{settings.BaseUrl}': {(int)probeResponse.StatusCode}");
        }

        return new AuthenticatedSession(client, roles);
    }

    private static async Task<string> RequestAccessTokenAsync(LoadTestSettings settings)
    {
        using var authClient = new HttpClient();
        using var response = await authClient.PostAsync(
            $"{settings.OidcAuthority}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = settings.OidcClientId,
                ["username"] = settings.Username,
                ["password"] = settings.Password,
            }));

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Load test login failed for '{settings.Username}' against '{settings.OidcAuthority}': {(int)response.StatusCode} {errorBody}");
        }

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("access_token").GetString()!;
    }

    // Decodes the token's payload segment without validating the signature - this
    // tool already trusts whatever Keycloak just handed it over TLS/plain HTTP; it
    // only needs to read the "role" claims to decide which load tests apply.
    private static List<string> ReadRoleClaims(string accessToken)
    {
        var payloadSegment = accessToken.Split('.')[1];
        var padded = payloadSegment.PadRight(payloadSegment.Length + (4 - payloadSegment.Length % 4) % 4, '=');
        var payloadBytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
        var payload = JsonDocument.Parse(payloadBytes).RootElement;

        if (!payload.TryGetProperty("role", out var roleElement))
        {
            return [];
        }

        return roleElement.ValueKind switch
        {
            JsonValueKind.Array => roleElement.EnumerateArray().Select(e => e.GetString()!).ToList(),
            JsonValueKind.String => [roleElement.GetString()!],
            _ => [],
        };
    }
}
