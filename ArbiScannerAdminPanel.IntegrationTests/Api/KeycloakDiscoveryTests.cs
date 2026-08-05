using System.Net;
using System.Net.Http.Headers;
using ArbiScannerAdminPanel.IntegrationTests.Fixtures;
using FluentAssertions;

namespace ArbiScannerAdminPanel.IntegrationTests.Api;

// The one test class in the suite that proves the API's resource-server config
// actually works against a live Keycloak instance: real Authority-based OIDC
// discovery, real JWKS signature validation, real realm import of the exact
// file that ships to production, and - unique to this realm - the flat "role"
// claim protocol mapper that [Authorize(Roles = ...)] depends on. Every other
// test uses the fast forged-JWT path (JwtTestSettings/JwtTestTokenFactory)
// instead.
[Collection(KeycloakCollectionDefinition.Name)]
public class KeycloakDiscoveryTests(KeycloakTestFixture fixture)
{
    [Fact]
    public async Task AdministratorOnlyEndpoint_WithRealKeycloakAdministratorToken_ReturnsOk()
    {
        var token = await fixture.GetAdministratorTokenAsync();
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/Subscriptions/GetAllSubscriptions?page=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdministratorOnlyEndpoint_WithRealKeycloakManagerToken_ReturnsForbidden()
    {
        var token = await fixture.GetManagerTokenAsync();
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync("/api/Subscriptions/CreateSubscription", JsonContent(new
        {
            type = "ShouldNotBeCreated",
            price = 1.0m,
            durationInDays = 30,
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllSubscriptions_WithoutToken_ReturnsUnauthorized()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/Subscriptions/GetAllSubscriptions?page=1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static System.Net.Http.Json.JsonContent JsonContent(object value) =>
        System.Net.Http.Json.JsonContent.Create(value);
}
