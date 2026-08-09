using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerWeb.Infrastructure.Extensions;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArbiScannerAdminPanel.Infrastructure.Services
{
    // Deletes the Keycloak identity behind a WebApp user (arbiscanner-web realm) via
    // Client Credentials + the Keycloak Admin REST API — called from UsersService
    // alongside the local WebAppUserRepository delete, so "delete user" actually frees
    // the account (and its email) up instead of only clearing the local shadow row
    // that WebApp's JIT provisioning would otherwise just recreate on next login.
    public class KeycloakUserService : IKeycloakUserService
    {
        private const string RealmName = "arbiscanner-web";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeycloakUserService> _logger;

        public KeycloakUserService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<KeycloakUserService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<Result> DeleteUserAsync(string userId)
        {
            var section = _configuration.GetSection("Keycloak:WebRealmAdmin");
            var authority = section["Authority"];
            var clientId = section["ClientId"];
            var clientSecret = section["ClientSecret"];

            if (string.IsNullOrWhiteSpace(authority))
            {
                _logger.LogError("Keycloak:WebRealmAdmin:Authority is not configured; cannot delete Keycloak user {UserId}", userId);
                return Result.Fail(TypedErrors.InternalError("Keycloak admin operations are not configured."));
            }

            try
            {
                var token = await GetAccessTokenAsync(authority, clientId, clientSecret);
                if (token is null)
                {
                    _logger.LogError("Failed to obtain a Keycloak admin token while deleting user {UserId}", userId);
                    return Result.Fail(TypedErrors.InternalError("Failed to authenticate with Keycloak for user deletion."));
                }

                var keycloakBaseUrl = authority[..authority.IndexOf("/realms/", StringComparison.Ordinal)];
                using var request = new HttpRequestMessage(HttpMethod.Delete, $"{keycloakBaseUrl}/admin/realms/{RealmName}/users/{userId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);

                // A 404 means the user is already gone from Keycloak (e.g. deleted manually,
                // or a retry after a previous partial failure) - treat that as success so the
                // local shadow row still gets cleaned up rather than staying stuck forever.
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                {
                    return Result.Ok();
                }

                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Keycloak returned {StatusCode} deleting user {UserId}: {Body}", response.StatusCode, userId, body);
                return Result.Fail(TypedErrors.InternalError($"Keycloak returned {(int)response.StatusCode} while deleting the user."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Keycloak user {UserId}", userId);
                return Result.Fail(TypedErrors.InternalError("Error communicating with Keycloak."));
            }
        }

        private async Task<string?> GetAccessTokenAsync(string authority, string? clientId, string? clientSecret)
        {
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId ?? string.Empty,
                ["client_secret"] = clientSecret ?? string.Empty,
            });

            var response = await _httpClient.PostAsync($"{authority}/protocol/openid-connect/token", form);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("access_token", out var tokenElement) ? tokenElement.GetString() : null;
        }
    }
}
