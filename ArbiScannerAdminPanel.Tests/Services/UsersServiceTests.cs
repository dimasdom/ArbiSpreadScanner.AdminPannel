using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Application.Services;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Domain.Models.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ArbiScannerAdminPanel.Tests.Services;

public class UsersServiceTests
{
    private readonly Mock<IAdminUsersRepository> _adminUsersRepository = new();
    private readonly Mock<IWebAppUserRepository> _webAppUserRepository = new();
    private readonly UsersService _sut;

    public UsersServiceTests()
    {
        _sut = new UsersService(_adminUsersRepository.Object, _webAppUserRepository.Object, NullLogger<UsersService>.Instance);
    }

    [Fact]
    public async Task DeleteClientUser_DelegatesToRepositoryAndReturnsOk()
    {
        var result = await _sut.DeleteClientUser("u1");

        result.IsSuccess.Should().BeTrue();
        _webAppUserRepository.Verify(w => w.DeleteUser("u1"), Times.Once);
    }

    [Fact]
    public async Task DeleteClientUsers_DelegatesToRepositoryAndReturnsOk()
    {
        var ids = new List<string> { "u1", "u2" };

        var result = await _sut.DeleteClientUsers(ids);

        result.IsSuccess.Should().BeTrue();
        _webAppUserRepository.Verify(w => w.DeleteUsers(ids), Times.Once);
    }

    [Fact]
    public async Task GetClientUserById_UserNotFound_ReturnsFail()
    {
        _webAppUserRepository.Setup(w => w.GetById("u1")).ReturnsAsync((WebAppUserDTO?)null);

        var result = await _sut.GetClientUserById("u1");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetClientUserById_Success_ReturnsDtoWithSubscriptionAndPayments()
    {
        _webAppUserRepository.Setup(w => w.GetById("u1")).ReturnsAsync(new WebAppUserDTO { Id = "u1", Email = "a@b.com", UserName = "alice" });
        var subscription = new UserSubscriptionPayment { Id = 1, UserId = "u1" };
        _adminUsersRepository.Setup(a => a.GetUserSubscriptionPaymentByUserId("u1")).ReturnsAsync(subscription);
        var payments = new List<PaymentModel> { new() { Id = 1 } };
        _adminUsersRepository.Setup(a => a.GetPaymentsByUserId("u1")).ReturnsAsync(payments);

        var result = await _sut.GetClientUserById("u1");

        result.IsSuccess.Should().BeTrue();
        result.Value.UserMail.Should().Be("a@b.com");
        result.Value.UserName.Should().Be("alice");
        result.Value.Subscription.Should().Be(subscription);
        result.Value.Payments.Should().BeEquivalentTo(payments);
    }

    [Fact]
    public async Task GetClientUsers_NegativePage_NormalizesToOne()
    {
        _webAppUserRepository.Setup(w => w.GetUsers(1, 20)).ReturnsAsync(new List<WebAppUserDTO>());

        var result = await _sut.GetClientUsers(-5);

        result.IsSuccess.Should().BeTrue();
        _webAppUserRepository.Verify(w => w.GetUsers(1, 20), Times.Once);
    }

    [Fact]
    public async Task GetClientUsers_MapsActiveSubscriptionFlag()
    {
        _webAppUserRepository.Setup(w => w.GetUsers(1, 20)).ReturnsAsync(new List<WebAppUserDTO>
        {
            new() { Id = "u1", Email = "active@test.com" },
            new() { Id = "u2", Email = "expired@test.com" },
            new() { Id = "u3", Email = "none@test.com" }
        });
        _adminUsersRepository.Setup(a => a.GetUserSubscriptionByUserId("u1"))
            .ReturnsAsync(new UserSubscriptionModel { EndDate = DateTime.UtcNow.AddDays(10) });
        _adminUsersRepository.Setup(a => a.GetUserSubscriptionByUserId("u2"))
            .ReturnsAsync(new UserSubscriptionModel { EndDate = DateTime.UtcNow.AddDays(-10) });
        _adminUsersRepository.Setup(a => a.GetUserSubscriptionByUserId("u3"))
            .ReturnsAsync((UserSubscriptionModel?)null);

        var result = await _sut.GetClientUsers();

        result.IsSuccess.Should().BeTrue();
        result.Value.Single(r => r.Id == "u1").IsActiveSubscription.Should().BeTrue();
        result.Value.Single(r => r.Id == "u2").IsActiveSubscription.Should().BeFalse();
        result.Value.Single(r => r.Id == "u3").IsActiveSubscription.Should().BeFalse();
    }

    [Fact]
    public async Task GetUsersByEmail_MapsRows()
    {
        _webAppUserRepository.Setup(w => w.SearchByEmail("test")).ReturnsAsync(new List<WebAppUserDTO>
        {
            new() { Id = "u1", Email = "test@test.com" }
        });
        _adminUsersRepository.Setup(a => a.GetUserSubscriptionByUserId("u1")).ReturnsAsync((UserSubscriptionModel?)null);

        var result = await _sut.GetUsersByEmail("test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.Id == "u1");
    }

    [Fact]
    public async Task GetUserSubscriptionByUserId_NotFound_ReturnsFail()
    {
        _adminUsersRepository.Setup(a => a.GetUserSubscriptionByUserId("u1")).ReturnsAsync((UserSubscriptionModel?)null);

        var result = await _sut.GetUserSubscriptionByUserId("u1");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSubscriptionByUserId_Found_ReturnsOk()
    {
        var sub = new UserSubscriptionModel { UserId = "u1" };
        _adminUsersRepository.Setup(a => a.GetUserSubscriptionByUserId("u1")).ReturnsAsync(sub);

        var result = await _sut.GetUserSubscriptionByUserId("u1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(sub);
    }

    [Fact]
    public async Task UpdateClientUser_DelegatesToRepositoryAndReturnsOk()
    {
        var dto = new ClientAccountDTO { Id = "u1", UserMail = "new@test.com", UserName = "new-name" };

        var result = await _sut.UpdateClientUser(dto);

        result.IsSuccess.Should().BeTrue();
        _webAppUserRepository.Verify(w => w.UpdateUser("u1", "new@test.com", "new-name"), Times.Once);
    }
}
