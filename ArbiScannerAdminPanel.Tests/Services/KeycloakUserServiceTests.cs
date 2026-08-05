using System.Net;
using System.Net.Http;
using ArbiScannerAdminPanel.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ArbiScannerAdminPanel.Tests.Services;

public class KeycloakUserServiceTests
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage>? OnRequest { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(OnRequest?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private readonly StubHttpMessageHandler _handler = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly Mock<ILogger<KeycloakUserService>> _logger = new();

    public KeycloakUserServiceTests()
    {
        _httpClientFactory.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(_handler));
    }

    private static IConfiguration ConfigWith(string? authority = "http://keycloak.local/realms/arbiscanner-web", string? clientId = "client", string? clientSecret = "secret")
    {
        var data = new Dictionary<string, string?>();
        if (authority != null) data["Keycloak:WebRealmAdmin:Authority"] = authority;
        if (clientId != null) data["Keycloak:WebRealmAdmin:ClientId"] = clientId;
        if (clientSecret != null) data["Keycloak:WebRealmAdmin:ClientSecret"] = clientSecret;
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private KeycloakUserService CreateSut(IConfiguration? configuration = null) =>
        new(configuration ?? ConfigWith(), _httpClientFactory.Object, _logger.Object);

    private static HttpResponseMessage TokenResponse(string token) => new(HttpStatusCode.OK)
    {
        Content = new StringContent($$"""{"access_token":"{{token}}","expires_in":300}""", System.Text.Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task DeleteUserAsync_MissingAuthority_ReturnsFailWithoutCallingKeycloak()
    {
        var sut = CreateSut(ConfigWith(authority: null));

        var result = await sut.DeleteUserAsync("u1");

        result.IsFailed.Should().BeTrue();
        _handler.OnRequest = _ => throw new InvalidOperationException("Should not call Keycloak");
    }

    [Fact]
    public async Task DeleteUserAsync_TokenRequestFails_ReturnsFail()
    {
        _handler.OnRequest = req => req.RequestUri!.AbsolutePath.Contains("openid-connect/token")
            ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
            : new HttpResponseMessage(HttpStatusCode.OK);

        var result = await CreateSut().DeleteUserAsync("u1");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_KeycloakReturnsNoContent_ReturnsOk()
    {
        _handler.OnRequest = req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("openid-connect/token"))
                return TokenResponse("tok");

            req.Method.Should().Be(HttpMethod.Delete);
            req.RequestUri!.AbsolutePath.Should().Be("/admin/realms/arbiscanner-web/users/u1");
            req.Headers.Authorization!.Parameter.Should().Be("tok");
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        };

        var result = await CreateSut().DeleteUserAsync("u1");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_KeycloakReturnsNotFound_TreatedAsSuccess()
    {
        _handler.OnRequest = req => req.RequestUri!.AbsolutePath.Contains("openid-connect/token")
            ? TokenResponse("tok")
            : new HttpResponseMessage(HttpStatusCode.NotFound);

        var result = await CreateSut().DeleteUserAsync("u1");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_KeycloakReturnsServerError_ReturnsFail()
    {
        _handler.OnRequest = req => req.RequestUri!.AbsolutePath.Contains("openid-connect/token")
            ? TokenResponse("tok")
            : new HttpResponseMessage(HttpStatusCode.InternalServerError);

        var result = await CreateSut().DeleteUserAsync("u1");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_HttpThrows_ReturnsFail()
    {
        _handler.OnRequest = _ => throw new HttpRequestException("network down");

        var result = await CreateSut().DeleteUserAsync("u1");

        result.IsFailed.Should().BeTrue();
    }
}
