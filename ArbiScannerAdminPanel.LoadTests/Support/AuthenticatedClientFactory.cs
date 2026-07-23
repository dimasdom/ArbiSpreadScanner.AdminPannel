using System.Net;
using System.Net.Http.Json;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerAdminPanel.LoadTests.Settings;

namespace ArbiScannerAdminPanel.LoadTests.Support;

internal sealed record AuthenticatedSession(HttpClient Client, AdminAccountDTO Account);

internal static class AuthenticatedClientFactory
{
    public static async Task<AuthenticatedSession> CreateAsync(LoadTestSettings settings)
    {
        var handler = new SocketsHttpHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = LoadRunner.MaxConcurrency(settings.QueriesPerMinute)
        };

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(settings.BaseUrl)
        };

        var loginResponse = await client.PostAsJsonAsync("/api/Account/Authenticate", new AdminAccountAuthenticateDTO
        {
            UserName = settings.Username,
            Password = settings.Password
        });

        loginResponse.EnsureSuccessStatusCode();

        var result = await loginResponse.Content.ReadFromJsonAsync<ApiResult<AdminAccountDTO>>(JsonOptions.CaseInsensitive);
        if (result is not { IsSuccess: true } || result.Value is null)
        {
            client.Dispose();
            throw new InvalidOperationException(
                $"Load test login failed for '{settings.Username}' at '{settings.BaseUrl}': {result?.Message ?? "no response body"}");
        }

        return new AuthenticatedSession(client, result.Value);
    }
}
