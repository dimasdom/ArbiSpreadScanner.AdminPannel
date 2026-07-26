using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Infrastructure.Repositories;
using ArbiScannerAdminPanel.Tests.Helpers;
using FluentAssertions;

namespace ArbiScannerAdminPanel.Tests.Repositories;

public class AdminRefreshTokenRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsToken()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new AdminRefreshTokenRepository(context);
        var token = new AdminRefreshTokenModel { UserId = "u1", TokenHash = "hash1", ExpiresAt = DateTime.UtcNow.AddDays(1) };

        await sut.AddAsync(token);
        await sut.SaveChangesAsync();

        context.RefreshTokens.Should().ContainSingle(t => t.TokenHash == "hash1");
    }

    [Fact]
    public async Task GetByTokenHashAsync_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var sut = new AdminRefreshTokenRepository(context);

        var result = await sut.GetByTokenHashAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTokenHashAsync_Found_ReturnsToken()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        context.Users.Add(new AdminUserModel { Id = "u1", UserName = "admin" });
        context.RefreshTokens.Add(new AdminRefreshTokenModel { UserId = "u1", TokenHash = "hash1", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        await context.SaveChangesAsync();
        var sut = new AdminRefreshTokenRepository(context);

        var result = await sut.GetByTokenHashAsync("hash1");

        result.Should().NotBeNull();
        result!.UserId.Should().Be("u1");
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyMatchingTokens()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        context.RefreshTokens.AddRange(
            new AdminRefreshTokenModel { UserId = "u1", TokenHash = "h1", ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new AdminRefreshTokenModel { UserId = "u1", TokenHash = "h2", ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new AdminRefreshTokenModel { UserId = "u2", TokenHash = "h3", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        await context.SaveChangesAsync();
        var sut = new AdminRefreshTokenRepository(context);

        var result = await sut.GetByUserIdAsync("u1");

        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.UserId == "u1");
    }

    [Fact]
    public async Task RevokeAsync_SetsRevocationFields()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var token = new AdminRefreshTokenModel { UserId = "u1", TokenHash = "h1", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();
        var sut = new AdminRefreshTokenRepository(context);
        var replacementId = Guid.NewGuid();

        await sut.RevokeAsync(token, "rotated", "1.2.3.4", replacementId);
        await sut.SaveChangesAsync();

        token.RevokedAt.Should().NotBeNull();
        token.RevocationReason.Should().Be("rotated");
        token.RevokedByIp.Should().Be("1.2.3.4");
        token.ReplacedByTokenId.Should().Be(replacementId);
    }

    [Fact]
    public async Task RevokeTokenFamilyAsync_RevokesOnlyActiveTokensForUser()
    {
        using var context = DbContextFactory.CreateAdminPanelDbContext();
        var active = new AdminRefreshTokenModel { UserId = "u1", TokenHash = "h1", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var alreadyRevoked = new AdminRefreshTokenModel { UserId = "u1", TokenHash = "h2", ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = DateTime.UtcNow.AddMinutes(-1) };
        var otherUser = new AdminRefreshTokenModel { UserId = "u2", TokenHash = "h3", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        context.RefreshTokens.AddRange(active, alreadyRevoked, otherUser);
        await context.SaveChangesAsync();
        var sut = new AdminRefreshTokenRepository(context);

        await sut.RevokeTokenFamilyAsync("u1", "logout");
        await sut.SaveChangesAsync();

        active.RevokedAt.Should().NotBeNull();
        active.RevocationReason.Should().Be("logout");
        otherUser.RevokedAt.Should().BeNull();
    }
}
