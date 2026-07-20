using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Application.Services;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerAdminPanel.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ArbiScannerAdminPanel.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<UserManager<AdminUserModel>> _userManager;
    private readonly Mock<SignInManager<AdminUserModel>> _signInManager;
    private readonly Mock<IAdminRefreshTokenRepository> _refreshTokenRepository;
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _userManager = MockHelpers.CreateUserManagerMock();
        _signInManager = MockHelpers.CreateSignInManagerMock(_userManager);
        _refreshTokenRepository = new Mock<IAdminRefreshTokenRepository>();
        _refreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<AdminRefreshTokenModel>())).Returns(Task.CompletedTask);
        _refreshTokenRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        _sut = new AccountService(
            _userManager.Object,
            _signInManager.Object,
            _refreshTokenRepository.Object,
            new Mock<IHttpContextAccessor>().Object,
            Options.Create(MockHelpers.CreateTestJwtOptions()),
            NullLogger<AccountService>.Instance);
    }

    [Fact]
    public async Task AuthenticateAdmin_UserNotFound_ReturnsUnauthorized()
    {
        _userManager.Setup(m => m.FindByNameAsync("admin")).ReturnsAsync((AdminUserModel?)null);

        var result = await _sut.AuthenticateAdmin(new AdminAccountAuthenticateDTO { UserName = "admin", Password = "pw" });

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticateAdmin_WrongPassword_ReturnsUnauthorized()
    {
        var user = new AdminUserModel { Id = "u1", UserName = "admin" };
        _userManager.Setup(m => m.FindByNameAsync("admin")).ReturnsAsync(user);
        _signInManager.Setup(s => s.CheckPasswordSignInAsync(user, "wrong", false)).ReturnsAsync(SignInResult.Failed);

        var result = await _sut.AuthenticateAdmin(new AdminAccountAuthenticateDTO { UserName = "admin", Password = "wrong" });

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticateAdmin_Success_ReturnsTokensAndStoresRefreshToken()
    {
        var user = new AdminUserModel { Id = "u1", UserName = "admin" };
        _userManager.Setup(m => m.FindByNameAsync("admin")).ReturnsAsync(user);
        _signInManager.Setup(s => s.CheckPasswordSignInAsync(user, "correct", false)).ReturnsAsync(SignInResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        var result = await _sut.AuthenticateAdmin(new AdminAccountAuthenticateDTO { UserName = "admin", Password = "correct" });

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Value.Roles.Should().Contain("Admin");
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<AdminRefreshTokenModel>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAccessToken_TokenNotFound_ReturnsUnauthorized()
    {
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>())).ReturnsAsync((AdminRefreshTokenModel?)null);

        var result = await _sut.RefreshAccessToken("raw-token");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAccessToken_RevokedTokenReused_RevokesFamilyAndReturnsUnauthorized()
    {
        var storedToken = new AdminRefreshTokenModel
        {
            UserId = "u1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>())).ReturnsAsync(storedToken);

        var result = await _sut.RefreshAccessToken("raw-token");

        result.IsFailed.Should().BeTrue();
        _refreshTokenRepository.Verify(r => r.RevokeTokenFamilyAsync("u1", "suspicious_reuse"), Times.Once);
    }

    [Fact]
    public async Task RefreshAccessToken_ExpiredNotRevoked_ReturnsUnauthorizedWithoutRevokingFamily()
    {
        var storedToken = new AdminRefreshTokenModel
        {
            UserId = "u1",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = null
        };
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>())).ReturnsAsync(storedToken);

        var result = await _sut.RefreshAccessToken("raw-token");

        result.IsFailed.Should().BeTrue();
        _refreshTokenRepository.Verify(r => r.RevokeTokenFamilyAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAccessToken_UserMissing_ReturnsNotFound()
    {
        var storedToken = new AdminRefreshTokenModel
        {
            UserId = "u1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            User = null
        };
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>())).ReturnsAsync(storedToken);

        var result = await _sut.RefreshAccessToken("raw-token");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAccessToken_Success_RevokesOldTokenAndReturnsNewOnes()
    {
        var user = new AdminUserModel { Id = "u1", UserName = "admin" };
        var storedToken = new AdminRefreshTokenModel
        {
            UserId = "u1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            User = user
        };
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>())).ReturnsAsync(storedToken);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        _refreshTokenRepository.Setup(r => r.RevokeAsync(storedToken, "token_reissued", It.IsAny<string?>(), It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.RefreshAccessToken("raw-token", "1.2.3.4");

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        _refreshTokenRepository.Verify(r => r.RevokeAsync(storedToken, "token_reissued", "1.2.3.4", It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task LogoutByToken_TokenNotFound_ReturnsOkWithoutRevoke()
    {
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>())).ReturnsAsync((AdminRefreshTokenModel?)null);

        var result = await _sut.LogoutByToken("u1", "raw-token");

        result.IsSuccess.Should().BeTrue();
        _refreshTokenRepository.Verify(r => r.RevokeAsync(It.IsAny<AdminRefreshTokenModel>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task LogoutByToken_TokenBelongsToDifferentUser_ReturnsOkWithoutRevoke()
    {
        var storedToken = new AdminRefreshTokenModel { UserId = "other-user" };
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>())).ReturnsAsync(storedToken);

        var result = await _sut.LogoutByToken("u1", "raw-token");

        result.IsSuccess.Should().BeTrue();
        _refreshTokenRepository.Verify(r => r.RevokeAsync(It.IsAny<AdminRefreshTokenModel>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task LogoutByToken_Success_RevokesToken()
    {
        var storedToken = new AdminRefreshTokenModel { UserId = "u1" };
        _refreshTokenRepository.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>())).ReturnsAsync(storedToken);
        _refreshTokenRepository.Setup(r => r.RevokeAsync(storedToken, "logout", It.IsAny<string?>(), null)).Returns(Task.CompletedTask);

        var result = await _sut.LogoutByToken("u1", "raw-token", "9.9.9.9");

        result.IsSuccess.Should().BeTrue();
        _refreshTokenRepository.Verify(r => r.RevokeAsync(storedToken, "logout", "9.9.9.9", null), Times.Once);
    }

    [Fact]
    public async Task GetCurrentAdmin_UserNotFound_ReturnsNotFound()
    {
        _userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync((AdminUserModel?)null);

        var result = await _sut.GetCurrentAdmin("u1");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentAdmin_Success_ReturnsRoles()
    {
        var user = new AdminUserModel { Id = "u1" };
        _userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin", "SuperAdmin" });

        var result = await _sut.GetCurrentAdmin("u1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Roles.Should().BeEquivalentTo(new[] { "Admin", "SuperAdmin" });
    }
}
