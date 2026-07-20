using ArbiScannerAdminPanel.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using StackExchange.Redis;

namespace ArbiScannerAdminPanel.Tests.Helpers;

internal static class MockHelpers
{
    internal static Mock<UserManager<AdminUserModel>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AdminUserModel>>();
#pragma warning disable CS8625
        return new Mock<UserManager<AdminUserModel>>(
            store.Object, null, null, null, null, null, null, null, null);
#pragma warning restore CS8625
    }

    internal static Mock<SignInManager<AdminUserModel>> CreateSignInManagerMock(
        Mock<UserManager<AdminUserModel>> userManagerMock)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AdminUserModel>>();
#pragma warning disable CS8625
        return new Mock<SignInManager<AdminUserModel>>(
            userManagerMock.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null, null, null, null);
#pragma warning restore CS8625
    }

    internal static (Mock<IConnectionMultiplexer> Multiplexer, Mock<IDatabase> Database) CreateRedisMocks()
    {
        var mockDb = new Mock<IDatabase>();
        mockDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var mockMultiplexer = new Mock<IConnectionMultiplexer>();
        mockMultiplexer
            .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDb.Object);

        return (mockMultiplexer, mockDb);
    }

    internal static JwtOptions CreateTestJwtOptions() => new()
    {
        SigningKey = "test-signing-key-that-is-at-least-32-chars-long",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };
}
