using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.API.Controllers;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerWeb.Infrastructure.Extensions;
using FluentAssertions;
using FluentResults;
using Moq;

namespace ArbiScannerAdminPanel.Tests.Controllers;

public class SubscriptionsControllerTests
{
    private readonly Mock<ISubscriptionService> _subscriptionService = new();
    private readonly SubscriptionsController _sut;

    public SubscriptionsControllerTests()
    {
        _sut = new SubscriptionsController(_subscriptionService.Object);
    }

    [Fact]
    public async Task GetAllSubscriptions_DelegatesToService()
    {
        _subscriptionService.Setup(s => s.GetAllSubscriptions(1)).ReturnsAsync(Result.Ok(new List<SubscriptionModel> { new() { Id = 1 } }));

        var actionResult = await _sut.GetAllSubscriptions(1);
        var response = (SerializableResult<List<SubscriptionModel>>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task GetSubscriptionById_DelegatesToService()
    {
        _subscriptionService.Setup(s => s.GetSubscriptionById(1)).ReturnsAsync(Result.Fail<SubscriptionModel>("not found"));

        var actionResult = await _sut.GetSubscriptionById(1);
        var response = (SerializableResult<SubscriptionModel>)actionResult.Value!;

        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSubscription_DelegatesToService()
    {
        _subscriptionService.Setup(s => s.CreateSubscription(It.IsAny<SubscriptionModel>())).ReturnsAsync(Result.Ok());

        var actionResult = await _sut.CreateSubscription(new SubscriptionModel { Type = "Basic", Price = 10, DurationInDays = 30 });
        var response = (SerializableResult)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSubscription_DelegatesToService()
    {
        _subscriptionService.Setup(s => s.UpdateSubscription(It.IsAny<SubscriptionModel>())).ReturnsAsync(Result.Ok());

        var actionResult = await _sut.UpdateSubscription(new SubscriptionModel { Id = 1 });
        var response = (SerializableResult)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteSubscriptionById_DelegatesToService()
    {
        _subscriptionService.Setup(s => s.DeleteSubscriptionsById(It.IsAny<List<int>>())).ReturnsAsync(Result.Ok());

        var actionResult = await _sut.DeleteSubscriptionById(new List<int> { 1 });
        var response = (SerializableResult)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSubscriptionByUserId_DelegatesToService()
    {
        _subscriptionService.Setup(s => s.GetUserSubscriptionByUserId("u1")).ReturnsAsync(Result.Ok(new UserSubscriptionModel { UserId = "u1" }));

        var actionResult = await _sut.GetUserSubscriptionByUserId("u1");
        var response = (SerializableResult<UserSubscriptionModel>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value!.UserId.Should().Be("u1");
    }

    [Fact]
    public async Task CreateUserSubscription_DelegatesToService()
    {
        var dto = new UserSubscriptionCreateDTO { UserEmail = "a@test.com", SubscriptionId = 1 };
        _subscriptionService.Setup(s => s.CreateUserSubscription(dto)).ReturnsAsync(Result.Ok(new UserSubscriptionModel { UserId = "u1" }));

        var actionResult = await _sut.CreateUserSubscription(dto);
        var response = (SerializableResult<UserSubscriptionModel>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllUserSubscriptions_DelegatesToService()
    {
        _subscriptionService.Setup(s => s.GetAllUserSubscriptions(1)).ReturnsAsync(Result.Ok(new List<UserSubscriptionRowDTO>()));

        var actionResult = await _sut.GetAllUserSubscriptions(1);
        var response = (SerializableResult<List<UserSubscriptionRowDTO>>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserSubscription_DelegatesToService()
    {
        _subscriptionService.Setup(s => s.UpdateUserSubscription(It.IsAny<UserSubscriptionModel>())).ReturnsAsync(Result.Ok());

        var actionResult = await _sut.UpdateUserSubscription(new UserSubscriptionModel { Id = 1 });
        var response = (SerializableResult)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserSubscriptionsById_DelegatesToService()
    {
        _subscriptionService.Setup(s => s.DeleteUserSubscriptionsById(It.IsAny<List<int>>())).ReturnsAsync(Result.Ok());

        var actionResult = await _sut.DeleteUserSubscriptionsById(new List<int> { 1 });
        var response = (SerializableResult)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSubscriptionById_DelegatesToService()
    {
        _subscriptionService.Setup(s => s.GetUserSubscriptionById(1)).ReturnsAsync(Result.Ok(new UserSubscriptionModel { Id = 1 }));

        var actionResult = await _sut.GetUserSubscriptionById(1);
        var response = (SerializableResult<UserSubscriptionModel>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value!.Id.Should().Be(1);
    }
}
