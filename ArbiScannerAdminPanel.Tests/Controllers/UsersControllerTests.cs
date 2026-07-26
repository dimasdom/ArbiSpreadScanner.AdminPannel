using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.API.Controllers;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using ArbiScannerWeb.Infrastructure.Extensions;
using FluentAssertions;
using FluentResults;
using Moq;

namespace ArbiScannerAdminPanel.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUsersService> _usersService = new();
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        _sut = new UsersController(_usersService.Object);
    }

    [Fact]
    public async Task GetClientUsers_DelegatesToService()
    {
        _usersService.Setup(s => s.GetClientUsers(1)).ReturnsAsync(Result.Ok(new List<ClientAccountTableRowDTO> { new() { Id = "u1" } }));

        var actionResult = await _sut.GetClientUsers(1);
        var response = (SerializableResult<List<ClientAccountTableRowDTO>>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task GetClientUserById_DelegatesToService()
    {
        _usersService.Setup(s => s.GetClientUserById("u1")).ReturnsAsync(Result.Fail<ClientAccountDTO>("not found"));

        var actionResult = await _sut.GetClientUserById("u1");
        var response = (SerializableResult<ClientAccountDTO>)actionResult.Value!;

        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateClientUser_DelegatesToService()
    {
        _usersService.Setup(s => s.UpdateClientUser(It.IsAny<ClientAccountDTO>())).ReturnsAsync(Result.Ok());

        var actionResult = await _sut.UpdateClientUser(new ClientAccountDTO { Id = "u1" });
        var response = (SerializableResult)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteClientUsers_DelegatesToService()
    {
        _usersService.Setup(s => s.DeleteClientUsers(It.IsAny<List<string>>())).ReturnsAsync(Result.Ok());

        var actionResult = await _sut.DeleteClientUsers(new List<string> { "u1" });
        var response = (SerializableResult)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSubscriptionByUserId_DelegatesToService()
    {
        _usersService.Setup(s => s.GetUserSubscriptionByUserId("u1")).ReturnsAsync(Result.Ok(new UserSubscriptionModel { UserId = "u1" }));

        var actionResult = await _sut.GetUserSubscriptionByUserId("u1");
        var response = (SerializableResult<UserSubscriptionModel>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
        response.Value!.UserId.Should().Be("u1");
    }

    [Fact]
    public async Task GetUsersByEmail_DelegatesToService()
    {
        _usersService.Setup(s => s.GetUsersByEmail("test")).ReturnsAsync(Result.Ok(new List<ClientAccountTableRowDTO>()));

        var actionResult = await _sut.GetUsersByEmail("test");
        var response = (SerializableResult<List<ClientAccountTableRowDTO>>)actionResult.Value!;

        response.IsSuccess.Should().BeTrue();
    }
}
