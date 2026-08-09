using System.Net;
using System.Net.Http.Json;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.IntegrationTests.Fixtures;
using ArbiScannerAdminPanel.IntegrationTests.Support;
using FluentAssertions;

namespace ArbiScannerAdminPanel.IntegrationTests.Api;

// AdminPanel no longer issues or validates its own tokens (Keycloak does, see
// KeycloakDiscoveryTests) - what's left to prove here is that [Authorize] /
// [Authorize(Roles = "Administrator")] still enforce the same access boundary
// they always have, now driven by the "role" claim a Keycloak-issued token
// carries instead of ASP.NET Core Identity's role store.
[Collection(AdminApiCollectionDefinition.Name)]
public class AuthorizationTests(AdminApiTestFixture fixture)
{
    [Fact]
    public async Task BareAuthorizeEndpoint_WithAdministratorToken_ReturnsOk()
    {
        var client = fixture.Factory.CreateAuthenticatedClient(roles: "Administrator");

        var response = await client.GetAsync("/api/Subscriptions/GetAllSubscriptions?page=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BareAuthorizeEndpoint_WithManagerToken_ReturnsOk()
    {
        var client = fixture.Factory.CreateAuthenticatedClient(roles: "Manager");

        var response = await client.GetAsync("/api/Subscriptions/GetAllSubscriptions?page=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BareAuthorizeEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/Subscriptions/GetAllSubscriptions?page=1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdministratorOnlyEndpoint_WithAdministratorToken_ReturnsOk()
    {
        var client = fixture.Factory.CreateAuthenticatedClient(roles: "Administrator");

        var response = await client.PostAsJsonAsync("/api/Subscriptions/CreateSubscription", new SubscriptionModel
        {
            Type = "IntegrationTestPlan",
            Price = 1.0m,
            DurationInDays = 30,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResult>(JsonOptions.CaseInsensitive);
        result!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AdministratorOnlyEndpoint_WithManagerToken_ReturnsForbidden()
    {
        // Deliberate: AdminService (ArbiScannerWebApp) authenticates with only the
        // Manager role specifically so it can never reach Administrator-only actions.
        var client = fixture.Factory.CreateAuthenticatedClient(roles: "Manager");

        var response = await client.PostAsJsonAsync("/api/Subscriptions/CreateSubscription", new SubscriptionModel
        {
            Type = "ShouldNotBeCreated",
            Price = 1.0m,
            DurationInDays = 30,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdministratorOnlyEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/Subscriptions/CreateSubscription", new SubscriptionModel
        {
            Type = "ShouldNotBeCreated",
            Price = 1.0m,
            DurationInDays = 30,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
