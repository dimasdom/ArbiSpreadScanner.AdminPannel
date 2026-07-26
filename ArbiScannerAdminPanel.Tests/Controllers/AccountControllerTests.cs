using System.Security.Claims;
using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.API.Controllers;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerAdminPanel.Tests.Helpers;
using ArbiScannerWeb.Infrastructure.Extensions;
using FluentAssertions;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ArbiScannerAdminPanel.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly AccountController _sut;
    private readonly DefaultHttpContext _httpContext = new();

    public AccountControllerTests()
    {
        _sut = new AccountController(_accountService.Object, Options.Create(MockHelpers.CreateTestJwtOptions()), NullLogger<AccountController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    [Fact]
    public async Task Authenticate_Success_AppendsCookiesAndClearsTokensFromBody()
    {
        _accountService.Setup(s => s.AuthenticateAdmin(It.IsAny<AdminAccountAuthenticateDTO>()))
            .ReturnsAsync(Result.Ok(new AdminAccountDTO { AccessToken = "at", RefreshToken = "rt", Token = "t" }));

        var actionResult = await _sut.Authenticate(new AdminAccountAuthenticateDTO { UserName = "admin", Password = "pw" });
        var response = (SerializableResult<AdminAccountDTO>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value!.AccessToken.Should().BeEmpty();
        response.Value!.RefreshToken.Should().BeEmpty();
        response.Value!.Token.Should().BeEmpty();
        _httpContext.Response.Headers.SetCookie.Should().HaveCount(2);
    }

    [Fact]
    public async Task Authenticate_Failure_DoesNotAppendCookies()
    {
        _accountService.Setup(s => s.AuthenticateAdmin(It.IsAny<AdminAccountAuthenticateDTO>()))
            .ReturnsAsync(Result.Fail<AdminAccountDTO>(TypedErrors.Unauthorized("bad creds")));

        var actionResult = await _sut.Authenticate(new AdminAccountAuthenticateDTO { UserName = "admin", Password = "wrong" });
        var response = (SerializableResult<AdminAccountDTO>)actionResult.Value!;

        response.IsSuccess.Should().BeFalse();
        _httpContext.Response.Headers.SetCookie.Should().BeEmpty();
    }

    [Fact]
    public async Task Refresh_NoTokenInBodyOrCookie_ReturnsBadRequest()
    {
        var actionResult = await _sut.Refresh(null);

        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Refresh_UsesCookieWhenBodyMissing_Success()
    {
        _httpContext.Request.Headers["Cookie"] = "adminpanel.refresh_token=cookie-token";
        _accountService.Setup(s => s.RefreshAccessToken("cookie-token", It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Ok(new AdminRefreshTokenResponse { AccessToken = "at", RefreshToken = "rt" }));

        var actionResult = await _sut.Refresh(null);
        var response = (SerializableResult<AdminRefreshTokenResponse>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value!.AccessToken.Should().BeEmpty();
        _httpContext.Response.Headers.SetCookie.Should().HaveCount(2);
    }

    [Fact]
    public async Task Refresh_UsesBodyToken_Failure_DoesNotAppendCookies()
    {
        _accountService.Setup(s => s.RefreshAccessToken("body-token", It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Fail<AdminRefreshTokenResponse>(TypedErrors.Unauthorized("invalid token")));

        var actionResult = await _sut.Refresh(new AdminRefreshTokenRequest { RefreshToken = "body-token" });
        var response = (SerializableResult<AdminRefreshTokenResponse>)actionResult.Value!;

        response.IsSuccess.Should().BeFalse();
        _httpContext.Response.Headers.SetCookie.Should().BeEmpty();
    }

    [Fact]
    public async Task Logout_NoUserIdentity_ReturnsUnauthorized()
    {
        var actionResult = await _sut.Logout(new AdminRefreshTokenRequest { RefreshToken = "rt" });

        actionResult.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Logout_WithUserAndToken_LogsOutAndClearsCookies()
    {
        SetAuthenticatedUser("u1");
        _httpContext.Request.Headers["Cookie"] = "adminpanel.access_token=x; adminpanel.refresh_token=y";
        _accountService.Setup(s => s.LogoutByToken("u1", "rt", It.IsAny<string?>())).ReturnsAsync(Result.Ok());

        var actionResult = await _sut.Logout(new AdminRefreshTokenRequest { RefreshToken = "rt" });
        var response = (SerializableResult)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        _accountService.Verify(s => s.LogoutByToken("u1", "rt", It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Logout_NoRefreshToken_DoesNotCallService()
    {
        SetAuthenticatedUser("u1");

        var actionResult = await _sut.Logout(null);
        var response = (SerializableResult)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        _accountService.Verify(s => s.LogoutByToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Me_NoUserIdentity_ReturnsUnauthorized()
    {
        var actionResult = await _sut.Me();

        actionResult.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Me_WithUserIdentity_ReturnsAdmin()
    {
        SetAuthenticatedUser("u1");
        _accountService.Setup(s => s.GetCurrentAdmin("u1")).ReturnsAsync(Result.Ok(new AdminAccountDTO { Token = "t" }));

        var actionResult = await _sut.Me();
        var response = (SerializableResult<AdminAccountDTO>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }

    private void SetAuthenticatedUser(string userId)
    {
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userId) }, "Test"));
    }
}
