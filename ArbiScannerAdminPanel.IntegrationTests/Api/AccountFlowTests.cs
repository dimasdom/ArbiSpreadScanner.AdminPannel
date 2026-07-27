using System.Net;
using System.Net.Http.Json;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerAdminPanel.IntegrationTests.Fixtures;
using ArbiScannerAdminPanel.IntegrationTests.Support;
using FluentAssertions;

namespace ArbiScannerAdminPanel.IntegrationTests.Api;

[Collection(AdminApiCollectionDefinition.Name)]
public class AccountFlowTests(AdminApiTestFixture fixture)
{
    [Fact]
    public async Task FullAuthLifecycle_Succeeds()
    {
        // Uses CreateSecureClient(): the access/refresh cookies are Secure, so the client's
        // CookieContainer only re-sends them on a base address it considers HTTPS.
        var client = fixture.Factory.CreateSecureClient();

        var loginResponse = await client.PostAsJsonAsync("/api/Account/Authenticate", new AdminAccountAuthenticateDTO
        {
            UserName = AdminApiTestFixture.AdminUserName,
            Password = AdminApiTestFixture.AdminPassword,
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResult<AdminAccountDTO>>(JsonOptions.CaseInsensitive);
        loginResult!.IsSuccess.Should().BeTrue();
        loginResult.Value!.AccessToken.Should().BeEmpty();
        loginResult.Value.RefreshToken.Should().BeEmpty();
        loginResult.Value.Roles.Should().Contain("Administrator");

        var meResponse = await client.GetAsync("/api/Account/Me");
        var meResult = await meResponse.Content.ReadFromJsonAsync<ApiResult<AdminAccountDTO>>(JsonOptions.CaseInsensitive);
        meResult!.IsSuccess.Should().BeTrue();

        var refreshResponse = await client.PostAsJsonAsync<AdminRefreshTokenRequest?>("/api/Account/Refresh", null);
        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<ApiResult<AdminRefreshTokenResponse>>(JsonOptions.CaseInsensitive);
        refreshResult!.IsSuccess.Should().BeTrue();

        var logoutResponse = await client.PostAsJsonAsync<AdminRefreshTokenRequest?>("/api/Account/Logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var postLogoutResponse = await client.GetAsync("/api/Account/Me");
        postLogoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticate_InvalidPassword_ReturnsFailure()
    {
        var client = fixture.Factory.CreateSecureClient();

        var response = await client.PostAsJsonAsync("/api/Account/Authenticate", new AdminAccountAuthenticateDTO
        {
            UserName = AdminApiTestFixture.AdminUserName,
            Password = "wrong-password",
        });

        var result = await response.Content.ReadFromJsonAsync<ApiResult<AdminAccountDTO>>(JsonOptions.CaseInsensitive);
        result!.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Me_WithoutAuth_ReturnsUnauthorized()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/Account/Me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
