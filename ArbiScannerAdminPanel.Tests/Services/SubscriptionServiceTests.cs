using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Application.Services;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerAdminPanel.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ArbiScannerAdminPanel.Tests.Services;

public class SubscriptionServiceTests
{
    private readonly Mock<ISubscriptionsRepository> _subscriptionsRepository = new();
    private readonly Mock<IWebAppUserRepository> _webAppUserRepository = new();
    private readonly Mock<IConnectionMultiplexer> _redis;
    private readonly Mock<IDatabase> _redisDb;
    private readonly SubscriptionService _sut;

    public SubscriptionServiceTests()
    {
        (_redis, _redisDb) = MockHelpers.CreateRedisMocks();
        _sut = new SubscriptionService(_subscriptionsRepository.Object, _webAppUserRepository.Object, _redis.Object, NullLogger<SubscriptionService>.Instance);
    }

    private void SetupRedisNull() =>
        _redisDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.Null);

    private void SetupRedisCached(UserSubscriptionModel sub) =>
        _redisDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)JsonConvert.SerializeObject(sub));

    [Fact]
    public async Task AssignSubscriptionToUser_SubscriptionNotFound_ReturnsFail()
    {
        _subscriptionsRepository.Setup(s => s.GetSubscriptionById(1, false)).ReturnsAsync((SubscriptionModel?)null);

        var result = await _sut.AssignSubscriptionToUser(new UserSubscriptionPayment { UserId = "u1", SubscriptionId = 1 });

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task AssignSubscriptionToUser_Success_CreatesUserSubscriptionAndClearsCache()
    {
        _subscriptionsRepository.Setup(s => s.GetSubscriptionById(1, false)).ReturnsAsync(new SubscriptionModel { Id = 1, DurationInDays = 30 });

        var result = await _sut.AssignSubscriptionToUser(new UserSubscriptionPayment { UserId = "u1", SubscriptionId = 1 });

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be("u1");
        _subscriptionsRepository.Verify(s => s.AddUserSubscription(It.IsAny<UserSubscriptionModel>()), Times.Once);
        _redisDb.Verify(d => d.KeyDeleteAsync("userSubscription:u1", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task CreateSubscription_AddsAndSaves()
    {
        var result = await _sut.CreateSubscription(new SubscriptionModel { Price = 5, Type = "Basic", DurationInDays = 30 });

        result.IsSuccess.Should().BeTrue();
        _subscriptionsRepository.Verify(s => s.AddSubscription(It.IsAny<SubscriptionModel>()), Times.Once);
        _subscriptionsRepository.Verify(s => s.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateUserSubscription_UserNotFound_ReturnsFail()
    {
        _webAppUserRepository.Setup(w => w.GetByEmail("missing@test.com")).ReturnsAsync((WebAppUserDTO?)null);

        var result = await _sut.CreateUserSubscription(new UserSubscriptionCreateDTO { UserEmail = "missing@test.com", SubscriptionId = 1 });

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserSubscription_SubscriptionNotFound_ReturnsFail()
    {
        _webAppUserRepository.Setup(w => w.GetByEmail("a@test.com")).ReturnsAsync(new WebAppUserDTO { Id = "u1", Email = "a@test.com" });
        _subscriptionsRepository.Setup(s => s.GetSubscriptionById(1, false)).ReturnsAsync((SubscriptionModel?)null);

        var result = await _sut.CreateUserSubscription(new UserSubscriptionCreateDTO { UserEmail = "a@test.com", SubscriptionId = 1 });

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserSubscription_Success_CreatesAndClearsCache()
    {
        _webAppUserRepository.Setup(w => w.GetByEmail("a@test.com")).ReturnsAsync(new WebAppUserDTO { Id = "u1", Email = "a@test.com" });
        _subscriptionsRepository.Setup(s => s.GetSubscriptionById(1, false)).ReturnsAsync(new SubscriptionModel { Id = 1, DurationInDays = 30 });

        var result = await _sut.CreateUserSubscription(new UserSubscriptionCreateDTO { UserEmail = "a@test.com", SubscriptionId = 1 });

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be("u1");
        _redisDb.Verify(d => d.KeyDeleteAsync("userSubscription:u1", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSubscriptionsById_RemovesAndSaves()
    {
        var subs = new List<SubscriptionModel> { new() { Id = 1 } };
        _subscriptionsRepository.Setup(s => s.GetSubscriptionsByIds(It.IsAny<List<int>>())).ReturnsAsync(subs);

        var result = await _sut.DeleteSubscriptionsById(new List<int> { 1 });

        result.IsSuccess.Should().BeTrue();
        _subscriptionsRepository.Verify(s => s.RemoveSubscriptions(subs), Times.Once);
        _subscriptionsRepository.Verify(s => s.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSubscriptionById_NotFound_ReturnsFail()
    {
        _subscriptionsRepository.Setup(s => s.GetSubscriptionById(1, false)).ReturnsAsync((SubscriptionModel?)null);

        var result = await _sut.GetSubscriptionById(1);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetSubscriptionById_Found_ReturnsOk()
    {
        _subscriptionsRepository.Setup(s => s.GetSubscriptionById(1, false)).ReturnsAsync(new SubscriptionModel { Id = 1 });

        var result = await _sut.GetSubscriptionById(1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSubscriptionById_NotFound_ReturnsFail()
    {
        _subscriptionsRepository.Setup(s => s.GetUserSubscriptionById(1, false)).ReturnsAsync((UserSubscriptionModel?)null);

        var result = await _sut.GetUserSubscriptionById(1);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSubscriptionByUserId_ValidCacheHit_ReturnsCachedWithoutRepositoryCall()
    {
        var cached = new UserSubscriptionModel { UserId = "u1", EndDate = DateTime.UtcNow.AddDays(5) };
        SetupRedisCached(cached);

        var result = await _sut.GetUserSubscriptionByUserId("u1");

        result.IsSuccess.Should().BeTrue();
        _subscriptionsRepository.Verify(s => s.GetLatestUserSubscriptionByUserId(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetUserSubscriptionByUserId_ExpiredCache_FallsBackToRepository()
    {
        var stale = new UserSubscriptionModel { UserId = "u1", EndDate = DateTime.UtcNow.AddDays(-1) };
        SetupRedisCached(stale);
        var fresh = new UserSubscriptionModel { UserId = "u1", EndDate = DateTime.UtcNow.AddDays(10) };
        _subscriptionsRepository.Setup(s => s.GetLatestUserSubscriptionByUserId("u1")).ReturnsAsync(fresh);

        var result = await _sut.GetUserSubscriptionByUserId("u1");

        result.IsSuccess.Should().BeTrue();
        _subscriptionsRepository.Verify(s => s.GetLatestUserSubscriptionByUserId("u1"), Times.Once);
    }

    [Fact]
    public async Task GetUserSubscriptionByUserId_NoCacheNoRepository_ReturnsFail()
    {
        SetupRedisNull();
        _subscriptionsRepository.Setup(s => s.GetLatestUserSubscriptionByUserId("u1")).ReturnsAsync((UserSubscriptionModel?)null);

        var result = await _sut.GetUserSubscriptionByUserId("u1");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSubscriptionByUserId_CachesWithTtlClampedToSevenDays()
    {
        SetupRedisNull();
        var farFuture = new UserSubscriptionModel { UserId = "u1", EndDate = DateTime.UtcNow.AddDays(30) };
        _subscriptionsRepository.Setup(s => s.GetLatestUserSubscriptionByUserId("u1")).ReturnsAsync(farFuture);

        Expiration sevenDays = TimeSpan.FromDays(7);
        _redisDb.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                It.Is<Expiration>(e => e.Equals(sevenDays)),
                It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await _sut.GetUserSubscriptionByUserId("u1");

        _redisDb.Verify(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
            It.Is<Expiration>(e => e.Equals(sevenDays)),
            It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSubscription_NotFound_ReturnsFail()
    {
        _subscriptionsRepository.Setup(s => s.GetSubscriptionById(1, true)).ReturnsAsync((SubscriptionModel?)null);

        var result = await _sut.UpdateSubscription(new SubscriptionModel { Id = 1 });

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSubscription_Success_UpdatesFields()
    {
        var existing = new SubscriptionModel { Id = 1, Price = 1, Type = "Old", DurationInDays = 10 };
        _subscriptionsRepository.Setup(s => s.GetSubscriptionById(1, true)).ReturnsAsync(existing);

        var result = await _sut.UpdateSubscription(new SubscriptionModel { Id = 1, Price = 99, Type = "New", DurationInDays = 60 });

        result.IsSuccess.Should().BeTrue();
        existing.Price.Should().Be(99);
        existing.Type.Should().Be("New");
        existing.DurationInDays.Should().Be(60);
    }

    [Fact]
    public async Task UpdateUserSubscription_NotFound_ReturnsFail()
    {
        _subscriptionsRepository.Setup(s => s.GetUserSubscriptionById(1, true)).ReturnsAsync((UserSubscriptionModel?)null);

        var result = await _sut.UpdateUserSubscription(new UserSubscriptionModel { Id = 1, UserId = "u1" });

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserSubscription_Success_UpdatesEndDateAndClearsCache()
    {
        var existing = new UserSubscriptionModel { Id = 1, UserId = "u1", EndDate = DateTime.UtcNow };
        _subscriptionsRepository.Setup(s => s.GetUserSubscriptionById(1, true)).ReturnsAsync(existing);
        var newEndDate = new DateTime(2030, 5, 20, 15, 30, 0, DateTimeKind.Utc);

        var result = await _sut.UpdateUserSubscription(new UserSubscriptionModel { Id = 1, UserId = "u1", EndDate = newEndDate });

        result.IsSuccess.Should().BeTrue();
        existing.EndDate.Should().Be(new DateTime(2030, 5, 20, 0, 0, 0, DateTimeKind.Utc));
        _redisDb.Verify(d => d.KeyDeleteAsync("userSubscription:u1", It.IsAny<CommandFlags>()), Times.Once);
    }
}
